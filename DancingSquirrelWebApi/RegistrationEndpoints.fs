module RegistrationEndpoints

open Falco
open System.Web
open Microsoft.AspNetCore.Identity
open Microsoft.Extensions.DependencyInjection

type RegisterModel = 
    {
        // it's okay this is capitalized. 
        Email : string
        Password : string
    }

let registerHandler (createScope : unit -> IServiceScope) : HttpHandler = fun ctx -> 
    task {
        use scope = createScope()
        //Resolve ASP .NET Core Identity with DI help
        use userManager = scope.ServiceProvider.GetService<UserManager<IdentityUser>>()
        let user = IdentityUser(Email = "benjaminkrug@yahoo.com", UserName = "bjkrug")
        let! result = userManager.CreateAsync(user, "passworD123!")

        let jsonResponse =
            match result.Succeeded with
            | true -> Response.withStatusCode 201 >> Response.ofJson "success"
            | _ ->
                // result.Errors contains stuff like:
                //  Passwords must have at least one non alphanumeric character.
                //  Passwords must have at least one digit ('0'-'9').
                //  Passwords must have at least one uppercase ('A'-'Z').
                let errors = result.Errors |> Seq.map (fun e -> e.Description)  |> List.ofSeq
                Response.withStatusCode 400 >> Response.ofJson errors
        return! jsonResponse ctx            
    }