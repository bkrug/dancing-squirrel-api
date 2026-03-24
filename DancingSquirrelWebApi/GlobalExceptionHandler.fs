module GlobalExceptionHandler

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open System.Net
open System
open System.Threading.Tasks

type ExHandler (next : RequestDelegate)  =
    member this.InvokeAsync (context : HttpContext) : Task =
        async{
            try
                do! next.Invoke(context) |> Async.AwaitTask
            with
            //| :? Exception as ex ->
            | ex ->
                printfn "SQL: %O" ex
                context.Response.StatusCode <- int HttpStatusCode.BadRequest
                do! context.Response.WriteAsync(ex.Message) |> Async.AwaitTask               
        } |> Async.StartAsTask :> Task