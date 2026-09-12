module Extensions

open System

let tryParseInt (inputString: string) =
    match Int32.TryParse inputString with
    | true, teacherId -> Some teacherId
    | _ -> None

let tryParseEnum<'T when 'T: (new: unit -> 'T) and 'T: struct and 'T :> Enum> (inputString: string) =
    match Enum.TryParse<'T> (inputString, true) with
    | true, value -> Some value
    | _ -> None

let tryParseTimeOnly (inputString: string) =
    match TimeOnly.TryParse inputString with
    | true, timeOnly -> Some timeOnly
    | _ -> None

let getOption inputVal = if isNull inputVal then None else Some inputVal

let getOptionFromLiar inputValue =
    match inputValue with
    | Some null -> None
    | conventionalOption -> conventionalOption

let emptyStringToNone stringOption =
    match stringOption with
    | Some "" -> None
    | otherOption -> otherOption    