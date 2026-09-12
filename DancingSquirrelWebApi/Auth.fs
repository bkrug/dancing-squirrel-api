module Auth

open Falco
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Authentication
open System.Security.Claims

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
let private ifAuthenticatedInRole
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

let private authScheme = CookieAuthenticationDefaults.AuthenticationScheme

let processAuthenticatedRequest (requestLogic : HttpHandler) : HttpHandler = fun ctx ->
    task {
        do! Request.ifAuthenticated authScheme requestLogic ctx
    }

let processAuthorizedRequest (rolesAllowed : list<string>) (requestLogic : HttpHandler) : HttpHandler = fun ctx ->
    task {
        do! ifAuthenticatedInRole authScheme rolesAllowed requestLogic ctx
    }

//TODO: Consider returning empty lists instead of errors when the user can't authenticate
//The endpoints that are supposed to use this are just supposed to report details of what the user is allowed to do,
//not actually do anything.
let getCurrentClaims (requestLogic : Result<seq<Claim>, unit> -> HttpHandler) : HttpHandler =
    Request.authenticate authScheme (fun authenticateResult ctx ->
        match authenticateResult.Succeeded with
        | true ->
            let claims =
                match authenticateResult.Principal with
                | null -> Seq.empty
                | principal -> principal.Claims
            requestLogic (Ok claims) ctx
        | false ->
            requestLogic (Error()) ctx
    )

let private processWithMatchingClaim
    (claimType : string)
    (claimValueParser : string -> 'a option)
    (requestLogic : 'a -> HttpHandler) : HttpHandler =
    Request.authenticate authScheme (fun authenticateResult ctx ->
        let foundValue =
            if authenticateResult.Succeeded && isNull authenticateResult.Principal = false then
                authenticateResult.Principal.Claims
                |> Seq.filter (fun c -> c.Type = claimType)
                |> Seq.map (fun c -> c.Value)
                |> Seq.tryHead
                |> Option.bind claimValueParser
            else
                None
        match foundValue with
        | None ->
            ctx.ForbidAsync()
        | Some value ->
            requestLogic value ctx
    )

let processWithUserId (requestLogic : string -> HttpHandler) : HttpHandler =
    processWithMatchingClaim
        ClaimTypes.NameIdentifier
        Extensions.getOptional
        requestLogic

let processWithTeacherId (requestLogic : int -> HttpHandler) : HttpHandler =
    processWithMatchingClaim
        GenericModels.TeacherIdClaim
        Extensions.tryParseInt
        requestLogic