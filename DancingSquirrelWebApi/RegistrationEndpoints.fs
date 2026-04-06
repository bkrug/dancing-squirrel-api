module RegistrationEndpoints

open System.Web
open System.Text.Json
open System.Threading.Tasks
open System.Security.Claims
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

let getClaimsPrincipal =
    let claims =
        seq {
            new Claim(ClaimTypes.Name, "user.Email");
            new Claim("FullName", "user.FullName");
            new Claim(ClaimTypes.Role, "Administrator");
        };

    let claimsIdentity = new ClaimsIdentity(
        claims, CookieAuthenticationDefaults.AuthenticationScheme);

    //I can't figure out how to use auth Properties with Falco
    let authProperties = new AuthenticationProperties (
        AllowRefresh = true

        //ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
        // The time at which the authentication ticket expires. A 
        // value set here overrides the ExpireTimeSpan option of 
        // CookieAuthenticationOptions set with AddCookie.

        //IsPersistent = true,
        // Whether the authentication session is persisted across 
        // multiple requests. When used with cookies, controls
        // whether the cookie's lifetime is absolute (matching the
        // lifetime of the authentication ticket) or session-based.

        //IssuedUtc = <DateTimeOffset>,
        // The time at which the authentication ticket was issued.

        //RedirectUri = <string>
        // The full path or absolute URI to be used as an http 
        // redirect response value.
    )

    new ClaimsPrincipal(claimsIdentity)

let loginUserWithClaimsHandler4 (loginUserAsync : string -> string -> bool -> bool -> Task<bool>): HttpHandler = fun ctx ->
    task {
        let! jsonString = Request.getBodyString ctx
        let loginData = JsonSerializer.Deserialize<LoginModel>(jsonString, defaultJsonOptions)

        let! loginResult = loginUserAsync loginData.Username loginData.Password false false

        let httpResponse =
            if loginResult
            then
                let claimsPrincipal = getClaimsPrincipal
                Response.signIn CookieAuthenticationDefaults.AuthenticationScheme claimsPrincipal //authProperties
                //C#
                // await HttpContext.SignInAsync(
                //     CookieAuthenticationDefaults.AuthenticationScheme, 
                //     new ClaimsPrincipal(claimsIdentity), 
                //     authProperties);
            else
                Response.withStatusCode 401 >> Response.ofJson "TODO - failure to authenticate"
        
        return! httpResponse ctx
    }