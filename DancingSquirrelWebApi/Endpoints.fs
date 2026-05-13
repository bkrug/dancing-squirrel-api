module Endpoints

open Auth
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
open System.Collections.Generic
open RegistrationEndpoints
open TrainingRequest.Endpoints
open TrainingRequest.Queries

let getEndpoints (wApp : WebApplication) =
    //Kind of an example of what not to do:
    //I'm leaving this set of functions in place as an example of what things look like if we insist on refusing to use classes.
    //Since all of these methods share the same dependency, passing this in as a constructor is actually what classes are for.
    let connStr = wApp.Configuration.GetConnectionString("DancingSquirrelDb")
    let ctxtFactory = ExternalDependencies.getDbContextFactory connStr
    let insertTrainingRequest = trainingRequestInsertionFactory ctxtFactory
    let selectSingleTrainingRequest = singleTrainingRequestSelectionFactory ctxtFactory
    let selectMultiTrainingRequests = multiTrainingRequestSelectionFactory ctxtFactory
    let countTrainingRequests = trainingRequestCounterFactory ctxtFactory
    let insertOnboardedClient = OnboardedClientInsertionFactory ctxtFactory

    //This is an alternative to the above.
    //Just a class for a bunch of related stuff with the same dependency
    let identityWrap = new UserAuthorizationWrapper(wApp.Services.CreateScope)
        
    //This list of endpoints available in our application
    let endpoints =
        [
            post "/api/trainingRequest" (createTrainingRequest insertTrainingRequest)
            get "/api/trainingRequest" (getTrainingRequests selectMultiTrainingRequests countTrainingRequests)
                |> OpenApi.query [
                    { Name = "page"; Type = typeof<int64>; Required = false }
                    { Name = "length"; Type = typeof<int64>; Required = false }
                ]
            get "/api/trainingRequest/{trainingRequestId:int}" (getSingleTrainingRequest selectSingleTrainingRequest)
                |> OpenApi.route [
                    { Name = "trainingRequestId"; Type = typeof<int64>; Required = true }
                ]
            post "/api/squirrel/trainingRequest/{trainingRequestId:int}" (onboardClient insertOnboardedClient selectSingleTrainingRequest)
            post "/api/user" (registerNewUserHandler identityWrap.CreateUserAsync)
                |> OpenApi.acceptsType typeof<RegisterModel>
            post "/api/authentication" (loginUserWithClaimsHandler identityWrap.LoginUserAsync)
                |> OpenApi.acceptsType typeof<LoginModel>
            delete "/api/authentication" (logoutUser identityWrap.LogoutUserAsync)
            get "/api/authentication" loginCheck
            get "/api/authorization/admin" adminCheck
        ]
    endpoints