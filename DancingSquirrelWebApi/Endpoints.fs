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
open Calendar.Models
open Calendar.Queries
open Calendar.Endpoints

let getEndpoints (wApp : WebApplication) =
    let connStr = wApp.Configuration.GetConnectionString("DancingSquirrelDb")
    let ctxtFactory = ExternalDependencies.getDbContextFactory connStr
    let trQueries: ITrainingRequestQueries = TrainingRequestQueries(ctxtFactory)
    let dtQueries: IDanceTypeQueries = DanceTypeQueries(ctxtFactory)
    let crQueries: ICalendarQueries = CalendarQueries(ctxtFactory)
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

            //Calendar
            post "/api/teacher/{teacherId}/availability" (createDefaultAvailabilityFromForm crQueries)
            put "/api/teacher/{teacherId}/availability/{availabilityId}" (editDefaultAvailabilityFromForm crQueries)

            //User Management
            get "api/user" (getUsers identityWrap)
                |> OpenApi.query [
                    { Name = "page"; Type = typeof<int64>; Required = false }
                    { Name = "length"; Type = typeof<int64>; Required = false }
                ]
            get "/api/user/self" (getSelfHandler identityWrap)
            get "/api/user/{userId}" (getUserHandler identityWrap)
                |> OpenApi.route [
                    { Name = "userId"; Type = typeof<string>; Required = true }
                ]
                |> OpenApi.acceptsType typeof<EditUserModel>
            get "api/role" (getAllRoles identityWrap)
            post "/api/firstuser" (registerFirstUserHandler identityWrap)
                |> OpenApi.acceptsType typeof<CreateUserModel>
            post "/api/user" (registerNewUserHandler identityWrap)
                |> OpenApi.acceptsType typeof<CreateUserModel>
            put "/api/user/self" (editSelfHandler identityWrap)
                |> OpenApi.acceptsType typeof<EditUserModel>
            put "/api/user/{userId}" (editUserHandler identityWrap)
                |> OpenApi.route [
                    { Name = "userId"; Type = typeof<string>; Required = true }
                ]
                |> OpenApi.acceptsType typeof<EditUserModel>
            put "/api/user/{userId}/role" (editUserRolesHandler identityWrap)
                |> OpenApi.route [
                    { Name = "userId"; Type = typeof<string>; Required = true }
                ]
                |> OpenApi.acceptsType typeof<seq<RoleModel>>
            post "/api/user/self/password" (resetOwnPassword identityWrap)
                |> OpenApi.acceptsType typeof<OwnPasswordResetModel>
            post "/api/user/{userId}/password" (resetUserPassword identityWrap)
                |> OpenApi.route [
                    { Name = "userId"; Type = typeof<string>; Required = true }
                ]
                |> OpenApi.acceptsType typeof<PasswordResetModel>
            delete "/api/user/{userId}" (deleteUser identityWrap)
                |> OpenApi.route [
                    { Name = "userId"; Type = typeof<string>; Required = true }
                ]

            //Authentication
            post "/api/authentication" (loginUserWithClaimsHandler identityWrap)
                |> OpenApi.acceptsType typeof<LoginModel>
            delete "/api/authentication" (logoutUser identityWrap.LogoutUserAsync)
            get "/api/authentication" getCurrentUserRoles
            get "/api/notauthorized" notAuthorized
        ]
    endpoints