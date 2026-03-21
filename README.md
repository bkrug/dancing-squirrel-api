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

Then I copied .vscode/launch.json and .vscode/settings.json from another project. These add support for debugging and default some F# editor features.