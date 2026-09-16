module Extensions

open System

let tryParseInt (inputString: string) =
    match Int32.TryParse inputString with
    | true, teacherId -> Some teacherId
    | _ -> None

let getOption inputVal = if isNull inputVal then None else Some inputVal

let getNoneFromNull inputValue =
    match inputValue with
    | Some null -> None
    | conventionalOption -> conventionalOption

let getNoneFromEmpty stringOption =
    match stringOption with
    | Some "" -> None
    | otherOption -> otherOption