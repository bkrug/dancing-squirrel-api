module Auth

open Falco
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Authentication

#region CopyOfFalcoCode
/// TODO: When a Falco version > 5.2.0 is released, we can probably delete this region.
/// 
/// This code is slightly altered from the Falco source in order to avoid a null-reference error.
/// The original code checked "authenticateResult.Prinicipal.IsInRole" when users were not authenticated at all.
/// 
/// Proceeds if the authentication status of current `IPrincipal` is true and
/// they exist in a list of roles.
///
/// The roles are checked using `ClaimsPrincipal.IsInRole`, so the role claim type
/// is determined by the authentication handler in use. For example, with JWT Bearer
/// authentication, the role claim type is typically "roles" or "role", but with
/// cookie authentication it may be different depending on how claims are set up.
///
/// Note: This function assumes that the authentication handler populates the user's
/// claims with their roles in a way that `ClaimsPrincipal.IsInRole` can check. Make
/// sure your authentication setup is configured accordingly for role-based authorization
/// to work with this function.
///
/// - `authScheme`: The authentication scheme to use when authenticating the request. This should match the scheme used in your authentication configuration.
/// - `roles`: A sequence of roles to check against the authenticated user's claims. If the user is in any of the specified roles, they will be allowed to proceed.
/// - `handleOk`: The `HttpHandler` to invoke if the user is authenticated and in one of the specified roles. If the user is not authenticated or not in any of the roles, a 403 Forbidden response will be returned.
let ifAuthenticatedInRole
    (authScheme : string)
    (roles : string seq)
    (handleOk : HttpHandler) : HttpHandler =
    Request.authenticate authScheme (fun authenticateResult ctx ->
        let isInRole = authenticateResult.Succeeded && isNull authenticateResult.Principal = false && Seq.exists authenticateResult.Principal.IsInRole roles
        match isInRole with
        | true ->
            handleOk ctx
        | _ ->
            ctx.ForbidAsync())
#endregion

let authScheme = CookieAuthenticationDefaults.AuthenticationScheme

let processAuthenticatedRequest (requestLogic : HttpHandler) : HttpHandler = fun ctx ->
    task {
        do! Request.ifAuthenticated authScheme requestLogic ctx
    }

let processAuthorizedRequest (rolesAllowed : list<string>) (requestLogic : HttpHandler) : HttpHandler = fun ctx ->
    task {
        do! ifAuthenticatedInRole authScheme rolesAllowed requestLogic ctx
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