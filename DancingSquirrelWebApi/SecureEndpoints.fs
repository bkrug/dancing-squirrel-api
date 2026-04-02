module SecureEnpoints

open Falco

let authScheme = "some.secure.scheme"

let secureResourceHandler : HttpHandler =
    let handleAuth : HttpHandler =
        Response.ofPlainText "hello authenticated user"

    Request.ifAuthenticated authScheme handleAuth