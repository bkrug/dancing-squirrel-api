module TaskResult

open System.Threading.Tasks
open Microsoft.FSharp.Core

//Bind two methods together that both return Task<Result<>>
let bind binder result = task { 
    let! vR = result
    match vR with
    | Ok    v -> return! binder v
    | Error m -> return  Error m 
}

//Bind two methods together when the earlier method returns Result<> and the other returns Task<Result<>>
let bindToTask binder result = task { 
    let vR = result
    match vR with
    | Ok    v -> return! binder v
    | Error m -> return  Error m 
}