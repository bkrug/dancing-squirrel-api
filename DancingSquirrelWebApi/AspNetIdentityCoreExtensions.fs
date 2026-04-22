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
    member this.AddAspNetIdentityAuthentication(securityConnectionString: string) =

        this.AddDbContext<SecurityDbContext>(fun options ->
            options.UseSqlite(securityConnectionString) |> ignore
            ) |> ignore

        this.AddIdentityCore<IdentityUser>()
            .AddSignInManager()
            .AddUserManager<UserManager<IdentityUser>>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<SecurityDbContext>() |> ignore

        this.AddAuthorizationBuilder()
            .AddPolicy("RequireAdminRole", fun policy -> policy.RequireRole("Admin") |> ignore ) |> ignore

        //this.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        // this.AddAuthentication("Identity.Application")
        //     .AddJwtBearer(fun jwtOptions ->
        //         jwtOptions.Authority <- "https://example.com";
        //         jwtOptions.Audience <- allowedOrigins[0];
        //     ) |> ignore

        this.AddDatabaseDeveloperPageExceptionFilter() |> ignore

        this.AddDefaultIdentity<IdentityUser>(fun options -> options.SignIn.RequireConfirmedAccount <- true)
            .AddEntityFrameworkStores<SecurityDbContext>() |> ignore

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

        this.ConfigureApplicationCookie(fun options ->
            // Cookie settings
            options.Cookie.HttpOnly <- true
            options.ExpireTimeSpan <- TimeSpan.FromMinutes(int64 60)

            options.LoginPath <- "/api/authentication"
            options.AccessDeniedPath <- String.Empty
            options.SlidingExpiration <- true
        ) |> ignore


let ensureIdentitySeedData (serviceProvider: IServiceProvider) =
    use scope = serviceProvider.CreateScope()
    let roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>()
    let userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>()
    let ensureRoleExists roleName =
        task {
            let! doesExist = roleManager.RoleExistsAsync roleName
            if doesExist = false
            then
                let newRole = new IdentityRole(roleName)
                let! createResult = roleManager.CreateAsync newRole
                if createResult.Errors |> Seq.length > 0
                then
                    printfn "Role Creation Error: %O" createResult.Errors
                else
                    printfn "Created role: %s" roleName
        }
    let ensureUserIsAdmin username =
        task {
            let! appUser = userManager.FindByNameAsync username
            if isNull appUser = false
            then 
                let! roleResult = userManager.AddToRoleAsync(appUser, "Admin")
                if roleResult.Errors |> Seq.length > 0
                then
                    printfn "Error adding user to role: %O" roleResult.Errors
                else
                    printfn "Added %s user to Admin role" username
        }
    let roleNames = seq {
        "Admin"
        "Onboarder"
    }
    for roleName in roleNames do
        ensureRoleExists roleName |> ignore
    ensureUserIsAdmin "bkrug" |> ignore
    ensureUserIsAdmin "bkrug2" |> ignore