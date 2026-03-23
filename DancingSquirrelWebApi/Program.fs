open Falco
open Falco.Routing
open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.HttpsPolicy
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

//#region Move this to a different file
type trainingRequest =
    {
        CaretakerName: string
    }

let helloWorldHandler : HttpHandler = fun ctx ->
    task {
        let! f : FormData = Request.getForm ctx
        let person =
            { CaretakerName = f.GetString ("caretakername", "") }
        let name = "Nameless One"
        let message = sprintf "Hello %s" name
        return! Response.ofPlainText message ctx
    }
//#endregion

[<Literal>]
let allowedOriginsPolicy = "DancingSquirrelOrigins"

let wbuilder = WebApplication.CreateBuilder()

wbuilder.Services.AddCors(fun options ->
    options.AddPolicy(
        allowedOriginsPolicy,
        fun policy -> policy.WithOrigins("http://localhost:3626").AllowAnyHeader().AllowAnyMethod() |> ignore
    ) |> ignore
) |> ignore

let wapp = wbuilder.Build()

let endpoints =
    [
        post "/api/request/create" helloWorldHandler
    ]

wapp.UseRouting() |> ignore
wapp.UseCors(allowedOriginsPolicy) |> ignore
wapp.UseFalco(endpoints)
    .Run(Response.withStatusCode 404 >> Response.ofPlainText "Not found")
