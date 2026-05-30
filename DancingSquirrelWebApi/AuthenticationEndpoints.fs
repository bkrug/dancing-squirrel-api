module Authentication.Endpoints

open System.Text.Json
open System.Threading.Tasks
open System.Security.Claims
open System.Collections.Generic
open Falco
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Identity
open GenericModels

type LoginModel =
    {
        Username: string
        Password: string
    }

let authScheme = CookieAuthenticationDefaults.AuthenticationScheme

let private getClaimsPrincipal (identityUser: IdentityUser, roles: IList<string>) =
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
        claims, CookieAuthenticationDefaults.AuthenticationScheme)

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
                    let authProperties = new AuthenticationProperties (
                        AllowRefresh = true,
                        ExpiresUtc = System.DateTimeOffset.UtcNow.AddHours(2),
                        IsPersistent = true,
                        IssuedUtc = System.DateTime.UtcNow
                    )
                    Response.signInOptions authScheme claimsPrincipal authProperties
                | false ->
                    Response.withStatusCode 401 >> Response.ofJson "TODO - failure to authenticate"
        
        return! httpResponse ctx
    }

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