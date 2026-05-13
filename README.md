This is a WebApi for managing an imaginary company that trains squirrels to work in the entertainment industry.
See also the companion repo for the ReactJS application: https://github.com/bkrug/dancing-squirrel-ui

# Initial Setup

This repo expects SQLite databases at path /Database/*.db
The databases are not stored in the repo, so create the database using the DDL in schema.sql
(https://www.codegenes.net/blog/how-to-create-a-db-file-in-sqlite3-using-a-schema-file/)

```
sqlite3 /Database/DancingSquirrel.db
(This takes you into the sqlite3 program)
read /Database/schema.sql
^Z
```

# How to create a new solution containing a Falco project from command line

https://learn.microsoft.com/en-us/dotnet/fsharp/get-started/get-started-command-line

Note that I earlier ran this command to add a new dotnet template for Falco projects
```
dotnet new install "Falco.Template::*"
```

Then I ran these commands to create the solution and project, and open VS Code.
```
mkdir dancing-squirrel-api
cd dancing-squirrel-api
dotnet new sln --name DancingSquirrel
dotnet new list
dotnet new falco -o DancingSquirrelWebApi
dotnet sln add DancingSquirrelWebApi/DancingSquirrelWebApi.fsproj
code .
```

See also this command if you want to create the subdirectory in fewer steps:
`dotnet new sln -o FSharpSample`

Then I copied these files from another project:
- .vscode/launch.json (adds support for debugging)
- .vscode/settings.json (default some F# editor features)
- {project}/Properties/launchSettings.json (set up the local runtime ports)

Then I installed SqlHydra: https://github.com/JordanMarr/SqlHydra?tab=readme-ov-file#quick-start

```
dotnet new tool-manifest
dotnet tool install --local SqlHydra.Cli
```

```
cd DancingSquirrelWebApi/
dotnet sqlhydra sqlite
```
Now, supply the command line tool with the connection string "Data Source=/home/bkrug/Repos/dancing-squirrel-api/Database/DancingSquirrel.db"

(from DancingSquirrelWebApi/ folder)
```
dotnet add package SqlHydra.Query
dotnet add package Microsoft.Data.Sqlite
```

# Useful commands

`bash ./Database/schemaGeneration.sh`
Use this command to after adding or removing objects from one of the databases.
It updates some DDL SQL scripts, so that we don't need to store the databases as part of the git repo, but can easily create blank versions of the databases.
Each time the debugger is started up, VS Code auto-runs the above script.

# Troubleshooting

See this source for data on path variables used in launch.json
https://code.visualstudio.com/docs/reference/variables-reference

To Kill a port use: `fuser -k -n tcp 5626`

To see who is using the database: `lsof | grep "/home/bkrug/Repos/dancing-squirrel-api/Database/DancingSquirrel.db"`

## SqlHydra issues

Don't use this syntax unless you add "getId" into the command. This causes Database locking issues for some reason.
```
    insertTask shared {
        for _ in Database.main.SquirrelTeacher do
            entity {
                SquirrelTeacherId = 1;
                SquirrelId = squirrelId;
                TeacherId = teacherId;
            }
    } |> ignore
```
This seems to work fine.
```
    insertTask shared {
        into Database.main.SquirrelTeacher
            entity {
                SquirrelTeacherId = 1;
                SquirrelId = squirrelId;
                TeacherId = teacherId;
            }
    } |> ignore
```

# Authentication

Setting up authentication often only happens at the beginning of a project with relatively minor changes thereafter.
It was hard for me to get used to it again for this project.

When refreshing your memory, I think you first want to create an endpoint or two that just care about Http Only Cookies.
Create a login endpoint that can create such a cookie
(https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0),
and edit the front-end code to include that cookie in future requests.
This will involve calling `.AllowCredentials()` in your Program.cs/fs file
(https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-10.0#credentials-in-cross-origin-requests).

Afterwards you can worry about adding users and roles.
Use this information to create a C# project with an AspNetCore Identity database:
https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-10.0&tabs=visual-studio
