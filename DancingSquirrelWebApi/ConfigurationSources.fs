module DancingSquirrelDebugging

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration.Json

let getSourceFiles (webApplicationBuilder : WebApplicationBuilder) =
    webApplicationBuilder.Configuration.Sources
        |> Seq.filter (fun elem ->
            match elem with
            | :? JsonConfigurationSource as derived1 -> true
            | _ -> false)
        |> Seq.map (fun elem -> elem :?> JsonConfigurationSource)
        |> Seq.map (fun elem -> elem.Path)