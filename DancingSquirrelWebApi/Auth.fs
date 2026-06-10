module Auth

open Falco
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Authentication

let authScheme = CookieAuthenticationDefaults.AuthenticationScheme

let processAuthenticatedRequest (requestLogic : HttpHandler) : HttpHandler = fun ctx ->
    task {
        do! Request.ifAuthenticated authScheme requestLogic ctx
    }

let processAuthorizedRequest (rolesAllowed : list<string>) (requestLogic : HttpHandler) : HttpHandler = fun ctx ->
    task {
        do! Request.ifAuthenticatedInRole authScheme rolesAllowed requestLogic ctx
    }

let getCurrentUserRoles (requestLogic : Result<seq<string>, unit> -> HttpHandler) : HttpHandler =
    Request.authenticate authScheme (fun authenticateResult ctx ->
        match authenticateResult.Succeeded with
        | true ->
            let roles =
                if isNull authenticateResult.Principal = false then
                    authenticateResult.Principal.Claims
                    |> Seq.filter (fun c -> c.Type = System.Security.Claims.ClaimTypes.Role)
                    |> Seq.map (fun c -> c.Value)
                else
                    Seq.empty
            requestLogic (Ok roles) ctx
        | false ->
            requestLogic (Error()) ctx
    )

let getCurrentUserId (requestLogic : string -> HttpHandler) : HttpHandler =
    Request.authenticate authScheme (fun authenticateResult ctx ->
        let foundUserId =
            if authenticateResult.Succeeded && isNull authenticateResult.Principal = false then
                authenticateResult.Principal.Claims
                |> Seq.filter (fun c -> c.Type = System.Security.Claims.ClaimTypes.NameIdentifier)
                |> Seq.map (fun c -> c.Value)
                |> Seq.tryHead
            else
                None
        match foundUserId with
        | None ->
            ctx.ForbidAsync()
        | Some null ->
            ctx.ForbidAsync()
        | Some userId ->
            requestLogic userId ctx
    )