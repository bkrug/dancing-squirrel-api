module Auth

open Falco
open Microsoft.AspNetCore.Authentication.Cookies

let authScheme = CookieAuthenticationDefaults.AuthenticationScheme

let processAuthenticatedRequest (requestLogic : Microsoft.AspNetCore.Http.HttpContext -> System.Threading.Tasks.Task) : HttpHandler = fun ctx ->
    task {
        return! Request.ifAuthenticated authScheme requestLogic ctx
    }