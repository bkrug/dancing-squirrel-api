This is a WebApi for managing a hypothetical company that trains squirrels.
See also the companion repo for the ReactJS application: https://github.com/bkrug/dancing-squirrel-ui

*Initial Setup*

This repo expects an SQLite database at path /Database/DancingSquirrel.db
The database is not stored in the repo, so create the database using the DDL in schema.sql
(https://www.codegenes.net/blog/how-to-create-a-db-file-in-sqlite3-using-a-schema-file/)

```
sqlite3 /Database/DancingSquirrel.db
(This takes you into the sqlite3 program)
read /Database/schema.sql
^Z
```

*How to create a new solution containing a Falco project from command line*

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
Supplied the command line tool with the connection string "Data Source=/home/bkrug/Repos/dancing-squirrel-api/Database/DancingSquirrel.db"

(from DancingSquirrelWebApi/ folder)
```
dotnet add package SqlHydra.Query
dotnet add package Microsoft.Data.Sqlite
```

*Useful commands*

`sqlite3 ./Database/DancingSquirrel.db '.schema' > ./Database/schema.sql`

*Troubleshooting*

See this source for data on path variables used in launch.json
https://code.visualstudio.com/docs/reference/variables-reference

To Kill a port use: `fuser -k -n tcp 5626`
