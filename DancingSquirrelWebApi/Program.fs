open AspNetIdentityCoreExtensions
open Endpoints
open Falco
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
open Microsoft.AspNetCore.Identity
open Microsoft.EntityFrameworkCore

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
    .AddSwaggerGen() |> ignore

let securityConnectionString = builder.Configuration.GetConnectionString("SecurityDb");
builder.Services.AddAspNetIdentityAuthentication(securityConnectionString) |> ignore

let wApp = builder.Build()

wApp.UseRouting() |> ignore
wApp.UseHttpsRedirection()
    .UseSwagger()
    .UseSwaggerUI() |> ignore
wApp.UseCors(allowedOriginsPolicy) |> ignore
wApp.UseMiddleware<ExHandler>() |> ignore
wApp.UseFalco(getEndpoints wApp)
    .Run(Response.withStatusCode 404 >> Response.ofPlainText "Not found")
