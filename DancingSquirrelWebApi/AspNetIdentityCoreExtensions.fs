module AspNetIdentityCoreExtensions

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
open Microsoft.AspNetCore.Authentication.JwtBearer
open System
open SecurityDbLayer

type IServiceCollection with
    member this.AddAspNetIdentityAuthentication(securityConnectionString: string, allowedOrigins: string[]) =
        this.AddDbContext<SecurityDbContext>(fun options ->
            options.UseSqlite(securityConnectionString) |> ignore
            ) |> ignore

        //this.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        // this.AddAuthentication("Identity.Application")
        //     .AddJwtBearer(fun jwtOptions ->
        //         jwtOptions.Authority <- "https://example.com";
        //         jwtOptions.Audience <- allowedOrigins[0];
        //     ) |> ignore

        this.AddDatabaseDeveloperPageExceptionFilter() |> ignore

        this.AddDefaultIdentity<IdentityUser>(fun options -> options.SignIn.RequireConfirmedAccount <- true)
            .AddEntityFrameworkStores<SecurityDbContext>() |> ignore
        //this.AddRazorPages() |> ignore

        this.Configure<IdentityOptions>(fun (options: IdentityOptions) ->
            // Password settings.
            options.Password.RequireDigit <- true
            options.Password.RequireLowercase <- true
            options.Password.RequireNonAlphanumeric <- true
            options.Password.RequireUppercase <- true
            options.Password.RequiredLength <- 6
            options.Password.RequiredUniqueChars <- 1

            // Lockout settings.
            options.Lockout.DefaultLockoutTimeSpan <- TimeSpan.FromMinutes(int64 5)
            options.Lockout.MaxFailedAccessAttempts <- 5
            options.Lockout.AllowedForNewUsers <- true

            // User settings.
            options.User.AllowedUserNameCharacters <- "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+"
            options.User.RequireUniqueEmail <- true
        ) |> ignore

        // this.ConfigureApplicationCookie(fun options ->
        //     // Cookie settings
        //     options.Cookie.HttpOnly <- true
        //     options.ExpireTimeSpan <- TimeSpan.FromMinutes(int64 5)

        //     options.LoginPath <- "/Identity/Account/Login"
        //     options.AccessDeniedPath <- "/Identity/Account/AccessDenied"
        //     options.SlidingExpiration <- true
        // ) |> ignore        