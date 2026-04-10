module Endpoints

open ExternalDependencies
open Falco.Routing
open Falco.OpenApi
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.HttpsPolicy
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration.Json
open Microsoft.AspNetCore.Identity
open Microsoft.EntityFrameworkCore
open RegistrationEndpoints
open SecureEnpoints
open TrainingRequestEndpoints

let getEndpoints (wApp : WebApplication) =
    //Prepare a set of dependencies that hide the messy outside world from our deterministic code
    let connStr = wApp.Configuration.GetConnectionString("DancingSquirrelDb")
    let curEnv = new DbGetter(connStr)

    let createUserAsync = fun user password ->
        task {
            use scope = wApp.Services.CreateScope()
            //Resolve ASP .NET Core Identity with DI help
            use userManager = scope.ServiceProvider.GetService<UserManager<IdentityUser>>()
            let creationResult = userManager.CreateAsync(user, password)
            return! creationResult
        }

    let loginUserAsync = fun (username: string) (password: string) (isPersistent: bool) (lockoutOnFailure: bool) ->
        task {
            use scope = wApp.Services.CreateScope()
            let signInManager = scope.ServiceProvider.GetService<SignInManager<IdentityUser>>()
            let userManager = scope.ServiceProvider.GetService<UserManager<IdentityUser>>()
            let! user = signInManager.UserManager.FindByNameAsync(username)
            let! isCorrectPassword = signInManager.UserManager.CheckPasswordAsync(user, password)
            let! roles = userManager.GetRolesAsync(user)
            return isCorrectPassword, user, roles
        }
    
    let logoutUserAsync = fun () ->
        task {
            use scope = wApp.Services.CreateScope()
            let signInManager = scope.ServiceProvider.GetService<SignInManager<IdentityUser>>()
            return! signInManager.SignOutAsync()
        }
        
    //This list of endpoints available in our application
    let endpoints =
        [
            post "/api/request/create" (createTrainingRequest curEnv)
            get "/api/totalyauthenticated" secureResourceHandler
            post "/api/security/register" (registerNewUserHandler createUserAsync)
                |> OpenApi.acceptsType typeof<RegisterModel>
            post "/api/authentication" (loginUserWithClaimsHandler loginUserAsync)
                |> OpenApi.acceptsType typeof<LoginModel>
            delete "/api/authentication" (logoutUser logoutUserAsync)
            get "/api/authentication" loginCheck
            get "/api/authorization/admin" adminCheck
        ]
    endpoints