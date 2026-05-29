module Registration.Endpoints

open System.Text.Json
open Falco
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

let private mapToGridUserModel (user: IdentityUser) : GridUserModel =
    { UserId = user.Id; Username = user.UserName; }

let registerFirstUserHandler (queries: IUserAuthorizationWrapper) : HttpHandler = fun ctx ->
    task {
        let! countResult = queries.CountUsers
        match countResult with
        | Ok 0 ->
            let! jsonString = Request.getBodyString ctx
            let registrationData = JsonSerializer.Deserialize<RegisterModel>(jsonString, defaultJsonOptions)
            let user = mapToIdentityUser registrationData
            let! userCreationResult =
                queries.CreateUserAsync user registrationData.Password
                |> TaskResult.bind (fun _ -> queries.AddToRoleAsync user "Admin")
            return! getFormCreateResponse userCreationResult ctx            
        | Ok _ ->
            let responseModel = getGenericValidationFailure "A user already exists. This endpoint can only be used to generate the first user. That user will always be an admin."
            return! (Response.withStatusCode 400 >> Response.ofJson responseModel) ctx
        | Error _ ->
            return! (Response.withStatusCode 500 >> Response.ofJson internalErrorResponse) ctx
    }

let registerNewUserHandler (queries: IUserAuthorizationWrapper) : HttpHandler = 
    Auth.processAuthenticatedRequest
        (fun ctx ->
            task {
                let! jsonString = Request.getBodyString ctx
                let registrationData = JsonSerializer.Deserialize<RegisterModel>(jsonString, defaultJsonOptions)
                let user = mapToIdentityUser registrationData
                let! userCreationResult = queries.CreateUserAsync user registrationData.Password
                return! getFormCreateResponse userCreationResult ctx            
            }
        )

let private editUserFields (queries: IUserAuthorizationWrapper) (editData: EditUserModel) (user: IdentityUser) =
    task {
        user.Email <- editData.Email
        user.PhoneNumber <- editData.PhoneNumber
        let! editResult = queries.EditUserAsync user        
        return
            match editResult with
            | Ok _ -> Ok()
            | Error identityError ->
                let failMsg =
                    match identityError.ValidationFailures with | None -> Seq.empty | Some s -> s
                    |> Seq.map (fun vFail -> vFail.Description)
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
                    |> TaskResult.bind (editUserFields queries editData)
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
                let transformationResult = userResult |> Result.map (Seq.map mapToGridUserModel)
                let! recordCountResult = queries.CountUsers
                let httpPagedResponse = getHttpPagedDataResponse transformationResult recordCountResult page pageLength
                return! httpPagedResponse ctx
            }
        )