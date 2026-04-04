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
open TrainingRequest

let getEndpoints (wApp : WebApplication) =
    let connStr = wApp.Configuration.GetConnectionString("DancingSquirrelDb")
    let curEnv = new DbGetter(connStr)
    let createUserAsync = fun user password ->
        task {
            use scope = wApp.Services.CreateScope()
            //Resolve ASP .NET Core Identity with DI help
            use userManager = scope.ServiceProvider.GetService<UserManager<IdentityUser>>()
            return! userManager.CreateAsync(user, password)
        }
    let endpoints =
        [
            post "/api/request/create" (createTrainingRequest curEnv)
            get "/api/totalyauthenticated" secureResourceHandler
            post "/api/register" (registerHandler createUserAsync)
                |> OpenApi.acceptsType typeof<RegisterModel>
        ]
    endpoints