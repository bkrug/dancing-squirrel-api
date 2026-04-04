module RegistrationEndpoints

open Falco
open System.Web
open System.Text.Json
open System.Threading.Tasks
open Microsoft.AspNetCore.Identity

type RegisterModel = 
    {
        Email : string
        Username: string
        //TODO: Better practice is to generate a one-time password upon creation. Not accept one from the user.
        Password : string
    }

let registerHandler (createUserAsync : IdentityUser -> string -> Task<IdentityResult>) : HttpHandler = fun ctx -> 
    task {
        let! jsonString = Request.getBodyString ctx
        let options: JsonSerializerOptions = JsonSerializerOptions()
        options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
        let registrationData = JsonSerializer.Deserialize<RegisterModel>(jsonString, options)

        let user = IdentityUser(Email = registrationData.Email, UserName = registrationData.Username)
        let! userCreationResult = createUserAsync user registrationData.Password

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