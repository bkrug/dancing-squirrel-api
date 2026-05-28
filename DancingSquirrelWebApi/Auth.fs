module Auth

open Falco
open GenericModels
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Identity
open Microsoft.Extensions.DependencyInjection
open System.Collections.Generic
open System.Linq

let authScheme = CookieAuthenticationDefaults.AuthenticationScheme

let processAuthenticatedRequest (requestLogic : HttpHandler) : HttpHandler = fun ctx ->
    task {
        do! Request.ifAuthenticated authScheme requestLogic ctx
    }

type IUserAuthorizationWrapper =
    abstract member CreateUserAsync: (IdentityUser -> string -> System.Threading.Tasks.Task<IdentityResult>) with get
    abstract member LoginUserAsync: (string -> string -> bool -> bool -> System.Threading.Tasks.Task<bool * IdentityUser * IList<string>>) with get
    abstract member LogoutUserAsync: (unit -> System.Threading.Tasks.Task<unit>) with get
    abstract member SelectMultiUsers: int -> int -> System.Threading.Tasks.Task<Result<seq<IdentityUser>, GenericModelResponse<string>>>
    abstract member CountUsers: System.Threading.Tasks.Task<Result<int, GenericModelResponse<string>>>

type UserAuthorizationWrapper(createScope: unit -> IServiceScope) =
    interface IUserAuthorizationWrapper with
        member _.CreateUserAsync = fun user password ->
            task {
                use scope = createScope()
                use userManager = scope.ServiceProvider.GetService<UserManager<IdentityUser>>()
                return! userManager.CreateAsync(user, password)
            }

        member _.LoginUserAsync = fun (username: string) (password: string) (_isPersistent: bool) (_lockoutOnFailure: bool) ->
            task {
                use scope = createScope()
                let signInManager = scope.ServiceProvider.GetService<SignInManager<IdentityUser>>()
                let userManager = scope.ServiceProvider.GetService<UserManager<IdentityUser>>()
                let! user = signInManager.UserManager.FindByNameAsync(username)
                if user = null
                then
                    let user: IdentityUser = null
                    let roles: IList<string> = List<string> [ ]
                    return false, user, roles
                else
                    let! isCorrectPassword = signInManager.UserManager.CheckPasswordAsync(user, password)
                    let! roles = userManager.GetRolesAsync(user)
                    return isCorrectPassword, user, roles
            }

        member _.LogoutUserAsync = fun () ->
            task {
                use scope = createScope()
                let signInManager = scope.ServiceProvider.GetService<SignInManager<IdentityUser>>()
                return! signInManager.SignOutAsync()
            }

        member _.SelectMultiUsers (skip: int) (length: int) =
            task {
                try
                    use scope = createScope()
                    use userManager = scope.ServiceProvider.GetService<UserManager<IdentityUser>>()
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
                    use scope = createScope()
                    use userManager = scope.ServiceProvider.GetService<UserManager<IdentityUser>>()
                    let count = userManager.Users.Count()
                    return Ok count
                with
                | ex ->
                    printfn "SQL: %O" ex
                    return Error internalErrorResponse
            }
