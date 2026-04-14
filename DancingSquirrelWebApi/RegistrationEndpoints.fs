module RegistrationEndpoints

open System.Web
open System.Text.Json
open System.Threading.Tasks
open System.Security.Claims
open System.Collections.Generic
open Falco
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Identity

//When adding authentiction to an app, start with HttpOnly cookie authentication.
//https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0
//
//Once you have successful use of HttpOnly cookies, worry about AspNetCore Identity later.

type RegisterModel = 
    {
        Email : string
        Username: string
        //TODO: Better practice is to generate a one-time password upon creation. Not accept one from the user.
        Password : string
    }

type LoginModel =
    {
        Username: string
        Password: string
    }

let defaultJsonOptions =
    let options : JsonSerializerOptions = JsonSerializerOptions()
    options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
    options

let registerNewUserHandler (createUserAsync : IdentityUser -> string -> Task<IdentityResult>) : HttpHandler = fun ctx -> 
    task {
        let! jsonString = Request.getBodyString ctx
        let registrationData = JsonSerializer.Deserialize<RegisterModel>(jsonString, defaultJsonOptions)

        let user = IdentityUser(Email = registrationData.Email, UserName = registrationData.Username)
        let! userCreationResult = createUserAsync user registrationData.Password

        let jsonResponse =
            match userCreationResult.Succeeded with
            | true -> Response.withStatusCode 201 >> Response.ofJson "success"
            | _ ->
                // result.Errors contains stuff like:
                //  Passwords must have at least one non alphanumeric character.
                //  Passwords must have at least one digit ('0'-'9').
                //  Passwords must have at least one uppercase ('A'-'Z').
                let errors = userCreationResult.Errors |> Seq.map (fun e -> e.Description)  |> List.ofSeq
                Response.withStatusCode 400 >> Response.ofJson errors

        return! jsonResponse ctx            
    }

let getClaimsPrincipal (identityUser: IdentityUser, roles: IList<string>) =
    let roleClaims = 
        roles
        |> Seq.map (fun role -> new Claim(ClaimTypes.Role, role))
    let claims =
        seq {
            new Claim(ClaimTypes.Name, identityUser.UserName);
            new Claim(ClaimTypes.Email, identityUser.Email);
        }
        |> Seq.append roleClaims

    let claimsIdentity = new ClaimsIdentity(
        claims, CookieAuthenticationDefaults.AuthenticationScheme);

    let authProperties = new AuthenticationProperties (
        AllowRefresh = true,
        ExpiresUtc = System.DateTimeOffset.UtcNow.AddMinutes(10),
        IsPersistent = true,
        IssuedUtc = System.DateTime.UtcNow
    )

    new ClaimsPrincipal(claimsIdentity)

let loginUserWithClaimsHandler (loginUserAsync : string -> string -> bool -> bool -> Task<bool * IdentityUser * IList<string>>): HttpHandler = fun ctx ->
    task {
        let! jsonString = Request.getBodyString ctx
        let loginData = JsonSerializer.Deserialize<LoginModel>(jsonString, defaultJsonOptions)

        let! isCorrectPassword, user, roles = loginUserAsync loginData.Username loginData.Password false false

        let httpResponse =
            match isCorrectPassword with
                | true ->
                    let claimsPrincipal = getClaimsPrincipal(user, roles)
                    Response.signIn CookieAuthenticationDefaults.AuthenticationScheme claimsPrincipal
                    //C# version -- I can't figure out how to use auth Properties with Falco:
                    // await HttpContext.SignInAsync(
                    //     CookieAuthenticationDefaults.AuthenticationScheme, 
                    //     new ClaimsPrincipal(claimsIdentity), 
                    //     authProperties);
                | false ->
                    Response.withStatusCode 401 >> Response.ofJson "TODO - failure to authenticate"
        
        return! httpResponse ctx
    }

let authScheme = CookieAuthenticationDefaults.AuthenticationScheme

let logoutUser (logoutUserAsync : unit -> Task<unit>) =
    Auth.processAuthenticatedRequest
        (fun ctx ->
            task {
                let! task = logoutUserAsync()
                return! Response.signOut authScheme ctx
            }
        )

let loginCheck =
    Auth.processAuthenticatedRequest
        (
            Response.ofPlainText "hello authenticated user"
        ) : HttpHandler

let adminCheck : HttpHandler =
    let handleAuthInRole : HttpHandler =
        Response.ofPlainText "hello admin"

    let rolesAllowed = [ "Admin" ]

    Request.ifAuthenticatedInRole authScheme rolesAllowed handleAuthInRole