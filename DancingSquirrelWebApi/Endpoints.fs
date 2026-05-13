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
open DanceType.Endpoints
open DanceType.Queries
open RegistrationEndpoints
open TrainingRequest.Endpoints
open TrainingRequest.Queries

let getEndpoints (wApp : WebApplication) =
    let connStr = wApp.Configuration.GetConnectionString("DancingSquirrelDb")
    let ctxtFactory = ExternalDependencies.getDbContextFactory connStr
    let trQueries = TrainingRequestQueries(ctxtFactory)
    let selectDanceTypes = danceTypeSelectorFactory ctxtFactory
    let selectTeachersByDanceType = teachersByDanceTypeSelectorFactory ctxtFactory
    let identityWrap = new UserAuthorizationWrapper(wApp.Services.CreateScope)
        
    //This list of endpoints available in our application
    let endpoints =
        [
            post "/api/trainingRequest" (createTrainingRequest trQueries.InsertTrainingRequest)
            get "/api/trainingRequest" (getTrainingRequests trQueries.SelectMultiTrainingRequests trQueries.CountTrainingRequests)
                |> OpenApi.query [
                    { Name = "page"; Type = typeof<int64>; Required = false }
                    { Name = "length"; Type = typeof<int64>; Required = false }
                ]
            get "/api/trainingRequest/{trainingRequestId:int}" (getSingleTrainingRequest trQueries.SelectSingleTrainingRequest)
                |> OpenApi.route [
                    { Name = "trainingRequestId"; Type = typeof<int64>; Required = true }
                ]
            post "/api/squirrel/trainingRequest/{trainingRequestId:int}" (onboardClient trQueries.InsertOnboardedClient trQueries.SelectSingleTrainingRequest)
            get "/api/danceType" (getDanceTypes selectDanceTypes)
            get "/api/danceType/{danceTypeId:int}/teacher" (getTeachersByDanceType selectTeachersByDanceType)
                |> OpenApi.route [
                    { Name = "danceTypeId"; Type = typeof<int64>; Required = true }
                ]
            post "/api/user" (registerNewUserHandler identityWrap.CreateUserAsync)
                |> OpenApi.acceptsType typeof<RegisterModel>
            post "/api/authentication" (loginUserWithClaimsHandler identityWrap.LoginUserAsync)
                |> OpenApi.acceptsType typeof<LoginModel>
            delete "/api/authentication" (logoutUser identityWrap.LogoutUserAsync)
            get "/api/authentication" loginCheck
            get "/api/authorization/admin" adminCheck
        ]
    endpoints