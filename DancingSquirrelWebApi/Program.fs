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
open System.IO
open System.Reflection

[<Literal>]
let allowedOriginsPolicy = "DancingSquirrelOrigins"

let builder = WebApplication.CreateBuilder()

let allowedOrigins = builder.Configuration.GetValue<string>("AllowedOrigins").Split(",")
builder.Services.AddCors(fun options ->
    options.AddPolicy(
        allowedOriginsPolicy,
        fun policy -> 
            policy.WithOrigins(allowedOrigins).AllowCredentials().AllowAnyHeader().AllowAnyMethod() |> ignore
    ) |> ignore
) |> ignore

builder.Services.AddEndpointsApiExplorer() |> ignore
builder.Services
    .AddFalcoOpenApi()
    .AddSwaggerGen() |> ignore

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(fun options ->
        options.ExpireTimeSpan <- TimeSpan.FromMinutes(int64 20)
        options.SlidingExpiration <- true
    ) |> ignore
builder.Services.AddAuthorization() |> ignore
//builder.Services.ConfigureIdentity() |> ignore

let securityConnectionString = builder.Configuration.GetConnectionString("SecurityDb");
builder.Services.AddAspNetIdentityAuthentication(securityConnectionString) |> ignore

let wApp = builder.Build()

ensureIdentitySeedData wApp.Services |> ignore

let executionPath = Assembly.GetExecutingAssembly().Location
printfn "Running on path %s" executionPath
let directories = String.Join(", ", Directory.GetDirectories(".."))
printfn "Directories are %s" directories
let path = @"../Database"
let directoryFiles = String.Join(", ", Directory.GetFiles(path))
printfn "Files at path %s are: %s" path directoryFiles
wApp.UseAuthentication() |> ignore
wApp.UseAuthorization() |> ignore
wApp.UseCookiePolicy(new CookiePolicyOptions( MinimumSameSitePolicy = SameSiteMode.Strict; ) ) |> ignore
wApp.UseRouting() |> ignore
wApp.UseHttpsRedirection()
    .UseSwagger()
    .UseSwaggerUI() |> ignore
wApp.UseCors(allowedOriginsPolicy) |> ignore
wApp.UseMiddleware<ExHandler>() |> ignore
wApp.UseFalco(getEndpoints wApp)
    .Run(Response.withStatusCode 404 >> Response.ofPlainText "Not found")