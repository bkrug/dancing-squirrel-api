open Falco
open Falco.Routing
open Falco.OpenApi
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
open NSwag.AspNetCore
open NSwag.Annotations
open NSwag.Generation
open NSwag.Collections

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

let endpoints =
    [
        post "/api/request/create" helloWorldHandler
            |> OpenApi.name "Fortune"
            |> OpenApi.summary "A mystic fortune teller"
            |> OpenApi.description "Get a glimpse into your future, if you dare."
    ]

[<Literal>]
let allowedOriginsPolicy = "DancingSquirrelOrigins"

let builder = WebApplication.CreateBuilder()

builder.Services.AddCors(fun options ->
    options.AddPolicy(
        allowedOriginsPolicy,
        fun policy -> policy.WithOrigins("http://localhost:3626").AllowAnyHeader().AllowAnyMethod() |> ignore
    ) |> ignore
) |> ignore

builder.Services.AddEndpointsApiExplorer() |> ignore
builder.Services
    .AddFalcoOpenApi()
    .AddOpenApiDocument(fun config ->
        config.DocumentName <- "Dancing Squirrel Api"
        config.Title <- "Dancing Squire Api v1"
        config.Version <- "v1"
    )
    //.AddSwaggerGen()
    |> ignore

let wApp = builder.Build()

wApp.UseRouting() |> ignore
wApp.UseOpenApi() |> ignore
wApp.UseSwaggerUi(fun config ->
    config.DocumentTitle <- "Dancing Squirrel Endpoints"
    config.Path <- "/swagger"
    config.DocumentPath <- "/swagger/{documentName}/swagger.json"
    config.DocExpansion <- "list"
) |> ignore
wApp.UseCors(allowedOriginsPolicy) |> ignore
wApp.UseFalco(endpoints)
    .Run(Response.withStatusCode 404 >> Response.ofPlainText "Not found")
