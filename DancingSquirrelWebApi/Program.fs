open AspNetIdentityCoreExtensions
open Endpoints
open Falco
open Falco.OpenApi
open GlobalExceptionHandler
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.HttpsPolicy
open Microsoft.AspNetCore.Identity
open Microsoft.Extensions.Configuration.Json
open Microsoft.Extensions.Logging
open Microsoft.EntityFrameworkCore
open System

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

// let securityConnectionString = builder.Configuration.GetConnectionString("SecurityDb");
// builder.Services.AddAspNetIdentityAuthentication(securityConnectionString, allowedOrigins) |> ignore
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(fun options ->
        options.ExpireTimeSpan <- TimeSpan.FromMinutes(int64 20)
        options.SlidingExpiration <- true
        options.AccessDeniedPath <- "/Forbidden/";
    ) |> ignore
builder.Services.AddAuthorization() |> ignore
//builder.Services.ConfigureIdentity() |> ignore

let wApp = builder.Build()

wApp.UseAuthentication() |> ignore
wApp.UseAuthorization() |> ignore
wApp.UseRouting() |> ignore
wApp.UseHttpsRedirection()
    .UseSwagger()
    .UseSwaggerUI() |> ignore
wApp.UseCors(allowedOriginsPolicy) |> ignore
wApp.UseMiddleware<ExHandler>() |> ignore
wApp.UseFalco(getEndpoints wApp)
    .Run(Response.withStatusCode 404 >> Response.ofPlainText "Not found")
