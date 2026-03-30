open DbEnv
open DbLayer
open Falco
open Falco.Routing
open Falco.OpenApi
open GlobalExceptionHandler
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.HttpsPolicy
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration.Json
open TrainingRequest

[<Literal>]
let allowedOriginsPolicy = "DancingSquirrelOrigins"

let builder = WebApplication.CreateBuilder()

let allowedOrigins = builder.Configuration.GetValue<string>("AllowedOrigins").Split(",")
builder.Services.AddCors(fun options ->
    options.AddPolicy(
        allowedOriginsPolicy,
        fun policy -> policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod() |> ignore
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

let connStrOld = "Data Source=/home/bkrug/Repos/dancing-squirrel-api/Database/DancingSquirrel.db;"
let connStr = builder.Configuration.GetConnectionString("DancingSquirrelDb")
let curEnv = new DbGetter(connStr)
let endpoints =
    [
        post "/api/request/create" (createTrainingRequest curEnv)
    ]

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
wApp.UseMiddleware<ExHandler>() |> ignore
wApp.UseFalco(endpoints)
    .Run(Response.withStatusCode 404 >> Response.ofPlainText "Not found")
