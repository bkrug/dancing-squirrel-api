module Auth

open Falco
open Microsoft.AspNetCore.Authentication.Cookies

let authScheme = CookieAuthenticationDefaults.AuthenticationScheme

let processAuthenticatedRequest (requestLogic : HttpHandler) : HttpHandler = fun ctx ->
    task {
        do! Request.ifAuthenticated authScheme requestLogic ctx
    }