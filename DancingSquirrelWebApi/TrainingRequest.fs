module TrainingRequest

open Falco
open Falco.Routing
open Falco.OpenApi

type trainingRequest =
    {
        CaretakerName: string
    }

let helloWorldHandler : HttpHandler = fun ctx ->
    task {
        let! f : FormData = Request.getForm ctx
        let person =
            { CaretakerName = f.GetString ("caretakername", "") }
        let name = "Nameless One"
        let message = sprintf "Hello %s" name
        return! Response.ofPlainText message ctx
    }