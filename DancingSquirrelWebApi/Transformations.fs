module Transformations

let tryParseInt (inputString: string) =
    match System.Int32.TryParse inputString with
    | true, teacherId -> Some teacherId
    | _ -> None