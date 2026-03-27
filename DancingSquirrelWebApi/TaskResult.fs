module TaskResult

let bind fRA vRA = task { 
    let! vR = vRA
    match vR with
    | Ok    v -> return! fRA v
    | Error m -> return  Error m 
}

let bindToTask fRA vRA = task { 
    let vR = vRA
    match vR with
    | Ok    v -> return! fRA v
    | Error m -> return  Error m 
}