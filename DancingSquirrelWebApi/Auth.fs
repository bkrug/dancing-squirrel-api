module Auth

open Falco
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Identity
open Microsoft.Extensions.DependencyInjection
open System.Collections.Generic

let authScheme = CookieAuthenticationDefaults.AuthenticationScheme

let processAuthenticatedRequest (requestLogic : HttpHandler) : HttpHandler = fun ctx ->
    task {
        do! Request.ifAuthenticated authScheme requestLogic ctx
    }

type UserAuthorizationWrapper(createScope: unit -> IServiceScope) =
    member this.CreateUserAsync = fun user password ->
        task {
            use scope = createScope()
            //Resolve ASP .NET Core Identity with DI help
            use userManager = scope.ServiceProvider.GetService<UserManager<IdentityUser>>()
            let creationResult = userManager.CreateAsync(user, password)
            return! creationResult
        }

    member this.LoginUserAsync = fun (username: string) (password: string) (isPersistent: bool) (lockoutOnFailure: bool) ->
        task {
            use scope = createScope()
            let signInManager = scope.ServiceProvider.GetService<SignInManager<IdentityUser>>()
            let userManager = scope.ServiceProvider.GetService<UserManager<IdentityUser>>()
            let! user = signInManager.UserManager.FindByNameAsync(username)
            if user = null
            then
                let user: IdentityUser = null
                let roles: IList<string> = List<string> [ ]
                return false, user, roles
            else
                let! isCorrectPassword = signInManager.UserManager.CheckPasswordAsync(user, password)
                let! roles = userManager.GetRolesAsync(user)
                return isCorrectPassword, user, roles
        }
    
    member this.LogoutUserAsync = fun () ->
        task {
            use scope = createScope()
            let signInManager = scope.ServiceProvider.GetService<SignInManager<IdentityUser>>()
            return! signInManager.SignOutAsync()
        }