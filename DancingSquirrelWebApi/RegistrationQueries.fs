module Registration.Queries

open GenericModels
open Microsoft.AspNetCore.Identity
open Microsoft.Extensions.DependencyInjection
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks

type IUserAuthorizationWrapper =
    abstract member CreateUserAsync: (IdentityUser -> string -> Task<Result<bool, GenericModelResponse<seq<IdentityError>>>>) with get
    abstract member EditUserAsync: (IdentityUser -> Task<IdentityResult>) with get
    abstract member AddToRoleAsync: (IdentityUser -> string -> Task<Result<bool, GenericModelResponse<seq<IdentityError>>>>) with get
    abstract member GetRoleAsync: (IdentityUser -> Task<IList<string>>)
    abstract member LoginUserAsync: (string -> string -> bool -> bool -> Task<bool * IdentityUser * IList<string>>) with get
    abstract member LogoutUserAsync: (unit -> Task<unit>) with get
    abstract member GetUserAsync: string -> Task<Result<IdentityUser, GenericModelResponse<string>>>
    abstract member SelectMultiUsers: int -> int -> Task<Result<seq<IdentityUser>, GenericModelResponse<string>>>
    abstract member CountUsers: Task<Result<int, GenericModelResponse<string>>>
    abstract member DeleteUserAsync: string -> Task<Result<GenericModelResponse<bool>, GenericModelResponse<string>>>
    abstract member UnlockUserAsync: string -> string -> Task<Result<GenericModelResponse<bool>, GenericModelResponse<string>>>

type UserAuthorizationWrapper(createScope: unit -> IServiceScope) =
    let scope = createScope()
    let userManager = scope.ServiceProvider.GetService<UserManager<IdentityUser>>()

    interface IUserAuthorizationWrapper with
        member _.CreateUserAsync = fun user password ->
            task {
                let! result = userManager.CreateAsync(user, password)
                return
                    match result.Succeeded with
                    | true -> Ok true
                    | false -> Error (getGenericValidationFailure result.Errors)
            }

        member _.EditUserAsync = fun user ->
            task { return! userManager.UpdateAsync(user) }

        member _.AddToRoleAsync = fun user role ->
            task {
                let! result = userManager.AddToRoleAsync(user, role)
                return
                    match result.Succeeded with
                    | true -> Ok true
                    | false -> Error (getGenericValidationFailure result.Errors)
            }            

        member _.GetRoleAsync = fun user ->
            task { return! userManager.GetRolesAsync(user) }

        member _.LoginUserAsync = fun (username: string) (password: string) (_isPersistent: bool) (_lockoutOnFailure: bool) ->
            task {
                let signInManager = scope.ServiceProvider.GetService<SignInManager<IdentityUser>>()
                let! user = signInManager.UserManager.FindByNameAsync(username)
                if user = null then
                    let user: IdentityUser = null
                    let roles: IList<string> = List<string> []
                    return false, user, roles
                else
                    let! isCorrectPassword = signInManager.UserManager.CheckPasswordAsync(user, password)
                    let! roles = signInManager.UserManager.GetRolesAsync(user)
                    return isCorrectPassword, user, roles
            }

        member _.LogoutUserAsync = fun () ->
            task {
                let signInManager = scope.ServiceProvider.GetService<SignInManager<IdentityUser>>()
                return! signInManager.SignOutAsync()
            }

        member _.GetUserAsync (userId: string) =
            task {
                try
                    let! user = userManager.FindByIdAsync(userId)
                    return
                        match user with
                        | null -> Error notFoundResponse
                        | _ -> Ok user
                with
                | ex ->
                    printfn "SQL: %O" ex
                    return Error internalErrorResponse
            }

        member _.SelectMultiUsers (skip: int) (length: int) =
            task {
                try
                    let users = userManager.Users.Skip(skip).Take(length) |> Seq.toList
                    return Ok (users :> seq<IdentityUser>)
                with
                | ex ->
                    printfn "SQL: %O" ex
                    return Error internalErrorResponse
            }

        member _.CountUsers =
            task {
                try
                    let count = userManager.Users.Count()
                    return Ok count
                with
                | ex ->
                    printfn "SQL: %O" ex
                    return Error internalErrorResponse
            }

        member _.UnlockUserAsync (userId: string) (newPassword: string) =
            task {
                try
                    let! user = userManager.FindByIdAsync(userId)
                    match user with
                    | null -> return Error notFoundResponse
                    | _ ->
                        let! resetToken = userManager.GeneratePasswordResetTokenAsync(user)
                        let! resetResult = userManager.ResetPasswordAsync(user, resetToken, newPassword)
                        if not resetResult.Succeeded then
                            return Error internalErrorResponse
                        else
                            let! _ = userManager.ResetAccessFailedCountAsync(user)
                            let! _ = userManager.SetLockoutEndDateAsync(user, System.Nullable<System.DateTimeOffset>())
                            return Ok getGenericSuccess
                with
                | ex ->
                    printfn "SQL: %O" ex
                    return Error internalErrorResponse
            }

        member _.DeleteUserAsync (userId: string) =
            task {
                try
                    let! user = userManager.FindByIdAsync(userId)
                    match user with
                    | null -> return Error notFoundResponse
                    | _ ->
                        let! result = userManager.DeleteAsync(user)
                        match result.Succeeded with
                        | true -> return Ok getGenericSuccess
                        | false ->
                            printfn "Delete user failed: %A" (result.Errors |> Seq.map (fun e -> e.Description))
                            return Error internalErrorResponse
                with
                | ex ->
                    printfn "SQL: %O" ex
                    return Error internalErrorResponse
            }
