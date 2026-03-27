module TaskResult

let bindFromTaskToTask fRA vRA = task { 
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