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

let registerFirstUserHandler (queries: IUserAuthorizationWrapper) : HttpHandler = fun ctx ->
    task {
        let! countResult = queries.CountUsers
        match countResult with
        | Error _ ->
            return! (Response.withStatusCode 500 >> Response.ofJson internalErrorResponse) ctx
        | Ok count when count > 0 ->
            let responseModel =
                {
                    IsSuccess = false
                    IsInternalError = false
                    ValidationFailures = Some "A user already exists. This endpoint can only be used to generate the first user. That user will always be an admin."
                }
            return! (Response.withStatusCode 400 >> Response.ofJson responseModel) ctx
        | Ok _ ->
            let! jsonString = Request.getBodyString ctx
            let registrationData = JsonSerializer.Deserialize<RegisterModel>(jsonString, defaultJsonOptions)
            let user = IdentityUser(Email = registrationData.Email, UserName = registrationData.Username, PhoneNumber = registrationData.PhoneNumber)
            let! createResult = queries.CreateUserAsync user registrationData.Password
            match createResult.Succeeded with
            | false ->
                let errors = createResult.Errors |> Seq.map (fun e -> e.Description) |> List.ofSeq
                return! (Response.withStatusCode 400 >> Response.ofJson errors) ctx
            | true ->
                let! roleResult = queries.AddToRoleAsync user "Admin"
                let jsonResponse =
                    match roleResult.Succeeded with
                    | true -> Response.withStatusCode 201 >> Response.ofJson "success"
                    | _ ->
                        let errors = roleResult.Errors |> Seq.map (fun e -> e.Description) |> List.ofSeq
                        Response.withStatusCode 400 >> Response.ofJson errors
                return! jsonResponse ctx
    }

let registerNewUserHandler (queries: IUserAuthorizationWrapper) : HttpHandler = 
    Auth.processAuthenticatedRequest
        (fun ctx ->
            task {
                let! jsonString = Request.getBodyString ctx
                let registrationData = JsonSerializer.Deserialize<RegisterModel>(jsonString, defaultJsonOptions)

                let user = IdentityUser(Email = registrationData.Email, UserName = registrationData.Username, PhoneNumber = registrationData.PhoneNumber)
                let! userCreationResult = queries.CreateUserAsync user registrationData.Password

                let jsonResponse =
                    match userCreationResult.Succeeded with
                    | true -> Response.withStatusCode 201 >> Response.ofJson "success"
                    | _ ->
                        // result.Errors contains stuff like:
                        //  Passwords must have at least one non alphanumeric character.
                        //  Passwords must have at least one digit ('0'-'9').
                        //  Passwords must have at least one uppercase ('A'-'Z').
                        let errors = userCreationResult.Errors |> Seq.map (fun e -> e.Description)  |> List.ofSeq
                        Response.withStatusCode 400 >> Response.ofJson errors

                return! jsonResponse ctx            
            }
        )

let editUserDeterministic (queries: IUserAuthorizationWrapper) (editData: EditUserModel) (user: IdentityUser) =
    task {
        user.Email <- editData.Email
        user.PhoneNumber <- editData.PhoneNumber
        let! editResult = queries.EditUserAsync user        
        return
            match editResult.Succeeded with
            | true -> Ok editResult
            | false -> Error {
                    IsInternalError = false
                    IsSuccess = false
                    ValidationFailures = Some (
                        editResult.Errors
                        |> Seq.map (fun identityError -> identityError.Description)
                        |> String.concat ", ")
                }
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

let getClaimsPrincipal (identityUser: IdentityUser, roles: IList<string>) =
    let roleClaims = 
        roles
        |> Seq.map (fun role -> new Claim(ClaimTypes.Role, role))
    let claims =
        seq {
            new Claim(ClaimTypes.Name, identityUser.UserName);
            new Claim(ClaimTypes.Email, identityUser.Email);
        }
        |> Seq.append roleClaims

    let claimsIdentity = new ClaimsIdentity(
        claims, CookieAuthenticationDefaults.AuthenticationScheme)

    new ClaimsPrincipal(claimsIdentity)

let authScheme = CookieAuthenticationDefaults.AuthenticationScheme

let loginUserWithClaimsHandler (loginUserAsync : string -> string -> bool -> bool -> Task<bool * IdentityUser * IList<string>>): HttpHandler = fun ctx ->
    task {
        let! jsonString = Request.getBodyString ctx
        let loginData = JsonSerializer.Deserialize<LoginModel>(jsonString, defaultJsonOptions)

        let! isCorrectPassword, user, roles = loginUserAsync loginData.Username loginData.Password false false

        let httpResponse =
            match isCorrectPassword with
                | true ->
                    let claimsPrincipal = getClaimsPrincipal(user, roles)
                    let authProperties = new AuthenticationProperties (
                        AllowRefresh = true,
                        ExpiresUtc = System.DateTimeOffset.UtcNow.AddHours(2),
                        IsPersistent = true,
                        IssuedUtc = System.DateTime.UtcNow
                    )
                    Response.signInOptions authScheme claimsPrincipal authProperties
                | false ->
                    Response.withStatusCode 401 >> Response.ofJson "TODO - failure to authenticate"
        
        return! httpResponse ctx
    }

let logoutUser (logoutUserAsync : unit -> Task<unit>) =
    Auth.processAuthenticatedRequest
        (fun ctx ->
            task {
                let! task = logoutUserAsync()
                return! Response.signOut authScheme ctx
            }
        )

let loginCheck =
    Auth.processAuthenticatedRequest
        (
            Response.ofPlainText "hello authenticated user"
        ) : HttpHandler

let adminCheck : HttpHandler =
    let handleAuthInRole : HttpHandler =
        Response.ofPlainText "hello admin"

    let rolesAllowed = [ "Admin" ]

    Request.ifAuthenticatedInRole authScheme rolesAllowed handleAuthInRole

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
                let! userResult = queries.GetUserAsync userId
                let viewModelResult =
                    userResult |> Result.map (fun user ->
                        {
                            UserId = user.Id;
                            Username = user.UserName;
                            Email = user.Email;
                            PhoneNumber = user.PhoneNumber
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
                let transformationResult : Result<seq<ViewUserModel>, GenericModelResponse<string>> =
                    match userResult with
                        | Ok userSeq ->
                            Ok (
                                userSeq
                                |> Seq.map (fun user ->
                                    {
                                        UserId = user.Id;
                                        Username = user.UserName;
                                        Email = user.Email;
                                        PhoneNumber = user.PhoneNumber;
                                    }
                                )
                            )
                        | Error e -> Error e
                let! recordCountResult = queries.CountUsers
                let httpPagedResponse = getHttpPagedDataResponse transformationResult recordCountResult page pageLength
                return! httpPagedResponse ctx
            }
        )