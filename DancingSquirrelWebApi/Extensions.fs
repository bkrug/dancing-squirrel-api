module Extensions

let tryParseInt (inputString: string) =
    match System.Int32.TryParse inputString with
    | true, teacherId -> Some teacherId
    | _ -> None

let getOptional inputVal = if isNull inputVal then None else Some inputVal