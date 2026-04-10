module TaskResult

//Bind two methods together that both return Task<Result<>>
let bind fRA vRA = task { 
    let! vR = vRA
    match vR with
    | Ok    v -> return! fRA v
    | Error m -> return  Error m 
}

//Bind two methods together when the earlier method returns Result<> and the other returns Task<Result<>>
let bindToTask fRA vRA = task { 
    let vR = vRA
    match vR with
    | Ok    v -> return! fRA v
    | Error m -> return  Error m 
}