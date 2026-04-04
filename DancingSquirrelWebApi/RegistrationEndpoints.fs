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

type LoginModel =
    {
        Username: string
        Password: string
    }

let defaultJsonOptions =
    let options : JsonSerializerOptions = JsonSerializerOptions()
    options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
    options

let registerNewUserHandler (createUserAsync : IdentityUser -> string -> Task<IdentityResult>) : HttpHandler = fun ctx -> 
    task {
        let! jsonString = Request.getBodyString ctx
        let registrationData = JsonSerializer.Deserialize<RegisterModel>(jsonString, defaultJsonOptions)

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

//TODO: This tests the password, but it doesn't return a token to the user
let loginUserHandler (loginUserAsync : string -> string -> bool -> bool -> Task<SignInResult>) : HttpHandler = fun ctx ->
    task {
        let! jsonString = Request.getBodyString ctx
        let loginData = JsonSerializer.Deserialize<LoginModel>(jsonString, defaultJsonOptions)

        let! loginResult = loginUserAsync loginData.Username loginData.Password false false

        let jsonResponse =
            match loginResult with
            | r when r.Succeeded = true -> Response.withStatusCode 200 >> Response.ofJson "TODO - success"
            | r when r.IsLockedOut = true -> Response.withStatusCode 400 >> Response.ofJson "TODO - locked out"
            | r when r.IsNotAllowed = true -> Response.withStatusCode 400 >> Response.ofJson "TODO - not allowed"
            | r when r.RequiresTwoFactor = true -> Response.withStatusCode 400 >> Response.ofJson "TODO - requires MFA"
            | _ -> Response.withStatusCode 400 >> Response.ofJson "TODO"
        
        return! jsonResponse ctx
    }