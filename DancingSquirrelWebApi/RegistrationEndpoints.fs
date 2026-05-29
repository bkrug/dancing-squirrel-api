module Registration.Endpoints

open System.Text.Json
open System.Threading.Tasks
open System.Security.Claims
open System.Collections.Generic
open Falco
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Identity
open GenericModels
open Registration.Models
open Registration.Queries

//When adding authentiction to an app, start with HttpOnly cookie authentication.
//https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0
//
//Once you have successful use of HttpOnly cookies, worry about AspNetCore Identity later.

let private mapToIdentityUser (data: RegisterModel) =
    IdentityUser(Email = data.Email, UserName = data.Username, PhoneNumber = data.PhoneNumber)

let private mapToViewUserModel (user: IdentityUser) (roleNames: seq<string>) : ViewUserModel =
    let roles = roleNames |> Seq.map (fun name -> { Name = name })
    { UserId = user.Id; Username = user.UserName; Email = user.Email; PhoneNumber = user.PhoneNumber; Roles = roles }

//This method isn't great. Maybe we can convert more user creation stuff to use Result objects.
let private identityResultToResponse successCode (result: IdentityResult) =
    match result.Succeeded with
    | true -> Response.withStatusCode successCode >> Response.ofJson "success"
    | _ ->
        let errors = result.Errors |> Seq.map (fun e -> e.Description) |> List.ofSeq
        Response.withStatusCode 400 >> Response.ofJson errors

let registerFirstUserHandler (queries: IUserAuthorizationWrapper) : HttpHandler = fun ctx ->
    task {
        let! countResult = queries.CountUsers
        match countResult with
        | Error _ ->
            return! (Response.withStatusCode 500 >> Response.ofJson internalErrorResponse) ctx
        | Ok count when count > 0 ->
            let responseModel = getGenericValidationFailure "A user already exists. This endpoint can only be used to generate the first user. That user will always be an admin."
            return! (Response.withStatusCode 400 >> Response.ofJson responseModel) ctx
        | Ok _ ->
            let! jsonString = Request.getBodyString ctx
            let registrationData = JsonSerializer.Deserialize<RegisterModel>(jsonString, defaultJsonOptions)
            let user = mapToIdentityUser registrationData
            let! createResult = queries.CreateUserAsync user registrationData.Password
            match createResult.Succeeded with
            | false -> return! (identityResultToResponse 201 createResult) ctx
            | true ->
                let! roleResult = queries.AddToRoleAsync user "Admin"
                return! (identityResultToResponse 201 roleResult) ctx
    }

let registerNewUserHandler (queries: IUserAuthorizationWrapper) : HttpHandler = 
    Auth.processAuthenticatedRequest
        (fun ctx ->
            task {
                let! jsonString = Request.getBodyString ctx
                let registrationData = JsonSerializer.Deserialize<RegisterModel>(jsonString, defaultJsonOptions)

                let user = mapToIdentityUser registrationData
                let! userCreationResult = queries.CreateUserAsync user registrationData.Password
                return! (identityResultToResponse 201 userCreationResult) ctx            
            }
        )

let private editUserDeterministic (queries: IUserAuthorizationWrapper) (editData: EditUserModel) (user: IdentityUser) =
    task {
        user.Email <- editData.Email
        user.PhoneNumber <- editData.PhoneNumber
        let! editResult = queries.EditUserAsync user        
        return
            match editResult.Succeeded with
            | true -> Ok editResult
            | false ->
                let failMsg =
                    editResult.Errors
                    |> Seq.map (fun identityError -> identityError.Description)
                    |> String.concat ", "
                Error (getGenericValidationFailure failMsg)
                        
    }

let editUserHandler (queries: IUserAuthorizationWrapper) : HttpHandler =
    Auth.processAuthenticatedRequest
        (fun ctx ->
            task {
                let userId = (Request.getRoute ctx).GetString "userId"
                let! jsonString = Request.getBodyString ctx
                let editData = JsonSerializer.Deserialize<EditUserModel>(jsonString, defaultJsonOptions)
                let! editResult =
                    Ok userId
                    |> TaskResult.bindToTask queries.GetUserAsync
                    |> TaskResult.bind (editUserDeterministic queries editData)
                let httpFormResponse = getFormEditResponse editResult
                return! httpFormResponse ctx
            }
        )

let unlockUser (queries: IUserAuthorizationWrapper) =
    Auth.processAuthenticatedRequest
        (fun ctx ->
            task {
                let userId = (Request.getRoute ctx).GetString "userId"
                let! jsonString = Request.getBodyString ctx
                let unlockData = JsonSerializer.Deserialize<UnlockUserModel>(jsonString, defaultJsonOptions)
                let! unlockResult = queries.UnlockUserAsync userId unlockData.Password
                let httpResponse = getHttpRecordResponse unlockResult
                return! httpResponse ctx
            }
        )

let deleteUser (queries: IUserAuthorizationWrapper) =
    Auth.processAuthenticatedRequest
        (fun ctx ->
            task {
                let userId = (Request.getRoute ctx).GetString "userId"
                let! deleteResult = queries.DeleteUserAsync userId
                let httpResponse = getHttpRecordResponse deleteResult
                return! httpResponse ctx
            }
        )

let getUserHandler (queries: IUserAuthorizationWrapper) =
    Auth.processAuthenticatedRequest
        (fun ctx ->
            task {
                let userId = (Request.getRoute ctx).GetString "userId"
                let! viewModelResult =
                    queries.GetUserAsync userId
                    |> TaskResult.bind (fun user -> task {
                        let! roleNames = queries.GetRoleAsync user
                        return Ok (mapToViewUserModel user roleNames)
                    })
                let httpResponse = getHttpRecordResponse viewModelResult
                return! httpResponse ctx
            }
        )

let getUsers (queries: IUserAuthorizationWrapper) =
    Auth.processAuthenticatedRequest
        (fun ctx ->
            task {
                let page = System.Math.Max(1, (Request.getQuery ctx).GetInt("page"))
                let pageLength = System.Math.Max(10, (Request.getQuery ctx).GetInt("length"))
                let skipCount = (page - 1) * pageLength
                let! userResult = queries.SelectMultiUsers skipCount pageLength
                let transformationResult = userResult |> Result.map (Seq.map (fun u -> mapToViewUserModel u Seq.empty))
                let! recordCountResult = queries.CountUsers
                let httpPagedResponse = getHttpPagedDataResponse transformationResult recordCountResult page pageLength
                return! httpPagedResponse ctx
            }
        )