open DbEnv
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
open Microsoft.AspNetCore.Identity
open Microsoft.EntityFrameworkCore
open System
open SecureEnpoints
open SecurityDbLayer
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

let securityConnectionString = builder.Configuration.GetConnectionString("SecurityDb");
builder.Services.AddDbContext<SecurityDbLayer.SecurityDbContext>(fun options ->
    options.UseSqlite(securityConnectionString) |> ignore
    ) |> ignore
builder.Services.AddDatabaseDeveloperPageExceptionFilter() |> ignore

builder.Services
    .AddDefaultIdentity<IdentityUser>(fun options -> options.SignIn.RequireConfirmedAccount <- true)
    .AddEntityFrameworkStores<SecurityDbContext>() |> ignore
builder.Services.AddRazorPages() |> ignore

builder.Services.Configure<IdentityOptions>(fun (options: IdentityOptions) ->
    // Password settings.
    options.Password.RequireDigit <- true
    options.Password.RequireLowercase <- true
    options.Password.RequireNonAlphanumeric <- true
    options.Password.RequireUppercase <- true
    options.Password.RequiredLength <- 6
    options.Password.RequiredUniqueChars <- 1

    // Lockout settings.
    options.Lockout.DefaultLockoutTimeSpan <- System.TimeSpan.FromMinutes(int64 5)
    options.Lockout.MaxFailedAccessAttempts <- 5
    options.Lockout.AllowedForNewUsers <- true

    // User settings.
    options.User.AllowedUserNameCharacters <- "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+"
    options.User.RequireUniqueEmail <- true
) |> ignore

builder.Services.ConfigureApplicationCookie(fun options ->
    // Cookie settings
    options.Cookie.HttpOnly <- true
    options.ExpireTimeSpan <- System.TimeSpan.FromMinutes(int64 5)

    options.LoginPath <- "/Identity/Account/Login"
    options.AccessDeniedPath <- "/Identity/Account/AccessDenied"
    options.SlidingExpiration <- true
) |> ignore

let connStr = builder.Configuration.GetConnectionString("DancingSquirrelDb")
let curEnv = new DbGetter(connStr)
let endpoints =
    [
        post "/api/request/create" (createTrainingRequest curEnv)
        get "/api/totalyauthenticated" secureResourceHandler
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
