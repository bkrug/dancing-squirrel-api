module Registration.Endpoints

open System.Collections.Generic
open System.Text.Json
open Falco
open Microsoft.AspNetCore.Identity
open GenericModels
open Registration.Models
open Registration.Queries
open ValidationStandards

//When adding authentiction to an app, start with HttpOnly cookie authentication.
//https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0
//
//Once you have successful use of HttpOnly cookies, worry about AspNetCore Identity later.

let private roles = [AdminRole]

let private mapToIdentityUser (data: CreateUserModel) =
    IdentityUser(Email = data.Email, UserName = data.Username, PhoneNumber = data.PhoneNumber)

let private mapToViewUserModel (user: IdentityUser) (roleNames: seq<string>) : ViewUserModel =
    let roles = roleNames |> Seq.map (fun name -> { Name = name })
    { UserId = user.Id; Username = user.UserName; Email = user.Email; PhoneNumber = user.PhoneNumber; Roles = roles }

let private mapToGridUserModel (user: IdentityUser) : GridUserModel =
    { UserId = user.Id; Username = user.UserName; Email = user.Email }

let private flattenIdentityError (result : Result<'a, GenericModelResponse<seq<IdentityError>>>) =
    match result with
    | Ok okVal -> Ok okVal
    | Error identityError ->
        let failMsg =
            match identityError.ValidationFailures with | None -> Seq.empty | Some s -> s
            |> Seq.map (fun vFail -> vFail.Description)
            |> String.concat ", "
        Error (getGenericValidationFailure failMsg)

let private replaceUnitSuccess (result: Result<unit, 'a>) =
    match result with
    | Ok _ -> Ok getGenericSuccess
    | Error err -> Error err

let private validateRequiredField (value: string) =
    match value with
    | "" -> Error requiredMessage
    | _ -> Ok()

let private getFieldValidationMessage keyName (validationResults: IDictionary<string, Result<unit, string>>) =
    match validationResults[keyName] with | Error msg -> msg | _ -> ""

let validateCreateUserModel (userModel: CreateUserModel) : GenericModelResponse<Result<unit, CreateUserModelValidation>> =
    let validationResults =
        dict [
            nameof userModel.Email,       validateEmail userModel.Email
            nameof userModel.Username,    validateRequiredField userModel.Username
            nameof userModel.Password,    validateRequiredField userModel.Password
            nameof userModel.PhoneNumber, validatePhone userModel.PhoneNumber
        ]
    let failureCount = validationResults |> Seq.filter (fun kvp -> kvp.Value.IsError) |> Seq.length
    match failureCount with
    | 0 -> getGenericSuccess
    | _ ->
        getGenericValidationFailure (Error {
            Username = getFieldValidationMessage (nameof userModel.Username) validationResults
            Password = getFieldValidationMessage (nameof userModel.Password) validationResults
            PhoneNumber = getFieldValidationMessage (nameof userModel.PhoneNumber) validationResults
            Email = getFieldValidationMessage (nameof userModel.Email) validationResults
        })

let registerFirstUserHandler (queries: IUserAuthorizationWrapper) : HttpHandler = fun ctx ->
    task {
        let! countResult = queries.CountUsers
        match countResult with
        | Ok 0 ->
            let! jsonString = Request.getBodyString ctx
            let registrationData = JsonSerializer.Deserialize<CreateUserModel>(jsonString, defaultJsonOptions)
            let user = mapToIdentityUser registrationData
            let! userCreationResult =
                queries.CreateUserAsync user registrationData.Password
                |> TaskResult.bind (fun _ -> queries.AddToRolesAsync ["Admin"] user)
            return! getFormCreateResponse userCreationResult ctx            
        | Ok _ ->
            let responseModel = getGenericValidationFailure "A user already exists. This endpoint can only be used to generate the first user. That user will always be an admin."
            return! (Response.withStatusCode 400 >> Response.ofJson responseModel) ctx
        | Error _ ->
            return! (Response.withStatusCode 500 >> Response.ofJson internalErrorResponse) ctx
    }

let registerNewUserHandler (queries: IUserAuthorizationWrapper) : HttpHandler = 
    Auth.processAuthorizedRequest roles
        (fun ctx ->
            task {
                let! jsonString = Request.getBodyString ctx
                let registrationData = JsonSerializer.Deserialize<CreateUserModel>(jsonString, defaultJsonOptions)
                let user = mapToIdentityUser registrationData
                let! userCreationResult = queries.CreateUserAsync user registrationData.Password
                return! getFormCreateResponse (userCreationResult |> replaceUnitSuccess) ctx            
            }
        )

let private editUserFields (queries: IUserAuthorizationWrapper) (editData: EditUserModel) (user: IdentityUser) =
    task {
        user.Email <- editData.Email
        user.PhoneNumber <- editData.PhoneNumber
        let! editResult = queries.EditUserAsync user        
        return editResult |> flattenIdentityError
    }

//TODO: This needs a more complicated authorization check.
//In order to call this, the user must either be an Admin, or the user must be editing their own data
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
                return! getFormEditResponse (editResult |> replaceUnitSuccess) ctx
            }
        )

let private updateUserRolesAsync (queries: IUserAuthorizationWrapper) (requestedRoles: seq<string>) (user: IdentityUser) =
    task {
        let! existingRoles = queries.GetRoleAsync user
        let addedRoles = requestedRoles |> Seq.except existingRoles
        let deletedRoles = existingRoles |> Seq.except requestedRoles
        let! updateResult = queries.UpdateUserRolesAsyncAsync addedRoles deletedRoles user
        return updateResult |> flattenIdentityError
    }

let editUserRolesHandler (queries: IUserAuthorizationWrapper) : HttpHandler =
    Auth.processAuthorizedRequest roles
        (fun ctx ->
            task {
                let userId = (Request.getRoute ctx).GetString "userId"
                let! jsonString = Request.getBodyString ctx
                let roleSeq = JsonSerializer.Deserialize<RoleEditingModel>(jsonString, defaultJsonOptions)
                let roleNameSeq = roleSeq.Roles |> Seq.map (fun role -> role.Name)
                let! editResult =
                    Ok userId
                    |> TaskResult.bindToTask queries.GetUserAsync
                    |> TaskResult.bind (updateUserRolesAsync queries roleNameSeq)
                return! getFormEditResponse (editResult |> replaceUnitSuccess) ctx                
            }
        )

let unlockUser (queries: IUserAuthorizationWrapper) =
    Auth.processAuthorizedRequest roles
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
    Auth.processAuthorizedRequest roles
        (fun ctx ->
            task {
                let userId = (Request.getRoute ctx).GetString "userId"
                let! deleteResult = queries.DeleteUserAsync userId
                let httpResponse = getHttpRecordResponse deleteResult
                return! httpResponse ctx
            }
        )

let getUserHandler (queries: IUserAuthorizationWrapper) =
    Auth.processAuthorizedRequest roles
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
    Auth.processAuthorizedRequest roles
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

let getRoles (queries: IUserAuthorizationWrapper) =
    Auth.processAuthorizedRequest roles
        (fun ctx ->
            let roleNames = queries.SelectAllRoles |> Seq.map (fun r -> r.Name) |> Seq.toList
            let roleCount = roleNames.Length
            let httpPagedResponse = getHttpPagedDataResponse (Ok roleNames) (Ok roleCount) 1 roleCount
            httpPagedResponse ctx
        )