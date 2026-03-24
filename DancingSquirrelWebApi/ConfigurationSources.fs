//We might never need this again,
//but I want to have something handy in case we strangely don't see the appsettings load correctly

module DancingSquirrelDebugging

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration.Json

let getSourceFiles (webApplicationBuilder : WebApplicationBuilder) =
    webApplicationBuilder.Configuration.Sources
        |> Seq.filter (fun elem -> elem :? JsonConfigurationSource)
        |> Seq.map (fun elem -> elem :?> JsonConfigurationSource)
        |> Seq.map (fun elem -> elem.Path)