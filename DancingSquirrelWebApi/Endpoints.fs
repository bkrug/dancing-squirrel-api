module Endpoints

open Authentication.Endpoints
open DanceType.Endpoints
open DanceType.Queries
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
open Registration.Models
open Registration.Queries
open Registration.Endpoints
open TrainingRequest.Endpoints
open TrainingRequest.Queries

let getEndpoints (wApp : WebApplication) =
    let connStr = wApp.Configuration.GetConnectionString("DancingSquirrelDb")
    let ctxtFactory = ExternalDependencies.getDbContextFactory connStr
    let trQueries: ITrainingRequestQueries = TrainingRequestQueries(ctxtFactory)
    let dtQueries: IDanceTypeQueries = DanceTypeQueries(ctxtFactory)
    let identityWrap: IUserAuthorizationWrapper = new UserAuthorizationWrapper(wApp.Services.CreateScope)

    //This list of endpoints available in our application
    let endpoints =
        [
            //Training Request
            post "/api/trainingRequest" (createTrainingRequest trQueries)
            get "/api/trainingRequest" (getTrainingRequests trQueries)
                |> OpenApi.query [
                    { Name = "page"; Type = typeof<int64>; Required = false }
                    { Name = "length"; Type = typeof<int64>; Required = false }
                ]
            get "/api/trainingRequest/{trainingRequestId:int}" (getSingleTrainingRequest trQueries)
                |> OpenApi.route [
                    { Name = "trainingRequestId"; Type = typeof<int64>; Required = true }
                ]
            post "/api/squirrel/trainingRequest/{trainingRequestId:int}" (onboardClient trQueries)
            get "/api/danceType" (getDanceTypes dtQueries)
            get "/api/danceType/{danceTypeId:int}/teacher" (getTeachersByDanceType dtQueries)
                |> OpenApi.route [
                    { Name = "danceTypeId"; Type = typeof<int64>; Required = true }
                ]

            //User Management
            post "/api/firstuser" (registerFirstUserHandler identityWrap)
                |> OpenApi.acceptsType typeof<CreateUserModel>
            post "/api/user" (registerNewUserHandler identityWrap)
                |> OpenApi.acceptsType typeof<CreateUserModel>
            put "/api/user/{userId}" (editUserHandler identityWrap)
                |> OpenApi.acceptsType typeof<EditUserModel>
            put "/api/user/{userId}/role" (editUserRolesHandler identityWrap)
                |> OpenApi.acceptsType typeof<seq<RoleModel>>
            post "/api/user/{userId}/unlock" (unlockUser identityWrap)
                |> OpenApi.route [
                    { Name = "userId"; Type = typeof<string>; Required = true }
                ]
            delete "/api/user/{userId}" (deleteUser identityWrap)
                |> OpenApi.route [
                    { Name = "userId"; Type = typeof<string>; Required = true }
                ]
            get "/api/user/{userId}" (getUserHandler identityWrap)
                |> OpenApi.route [
                    { Name = "userId"; Type = typeof<string>; Required = true }
                ]
            get "api/user" (getUsers identityWrap)
                |> OpenApi.query [
                    { Name = "page"; Type = typeof<int64>; Required = false }
                    { Name = "length"; Type = typeof<int64>; Required = false }
                ]
            get "api/role" (getRoles identityWrap)

            //Authentication
            post "/api/authentication" (loginUserWithClaimsHandler identityWrap.LoginUserAsync)
                |> OpenApi.acceptsType typeof<LoginModel>
            delete "/api/authentication" (logoutUser identityWrap.LogoutUserAsync)
            get "/api/authentication" loginCheck
            get "/api/authorization/admin" adminCheck
            get "api/notauthorized" notAuthorized
        ]
    endpoints