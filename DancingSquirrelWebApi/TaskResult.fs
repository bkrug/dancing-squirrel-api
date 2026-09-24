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

//Map the error component of a Task<Result<>>
let mapError mapper result = task {
    let! vR = result
    return vR |> Result.mapError mapper
}

//Map the success component of a Task<Result<>>
let map mapper result = task {
    let! vR = result
    return vR |> Result.map mapper
}

//Perform a side effect on the success value of a Task<Result<>>, leaving the result unchanged
let iterTask action result = task {
    let! vR = result
    match vR with
    | Ok v    -> do! action v
    | Error _ -> ()
    return vR
}

//Perform a side effect on the success value of a Task<Result<>>, leaving the result unchanged
let iter action result = task {
    let! vR = result
    vR |> Result.iter action
    return vR
}