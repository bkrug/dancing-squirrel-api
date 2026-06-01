module ValidationStandards

open System.Text.RegularExpressions

[<Literal>]
let requiredMessage = "is required"

let private emailRegex = Regex @"^[\w\-\.]+@([\w-]+\.)+[\w-]{2,}$"
let private unitedStatePhoneRegex = Regex @"^1?([^\d]*\d){10}[^\d]*$"
let private containsLetterRegex = Regex @"[a-zA-Z]+"

let validateEmail (value : string) =
    match value with
    | var1 when emailRegex.IsMatch var1 -> Ok()
    | "" -> Error requiredMessage
    | _ -> Error "must be an email address"

let validatePhone (value : string) =
    match value with
    | "" -> Ok()
    | var1 when containsLetterRegex.IsMatch var1 -> Error "must not contain letters"
    | var1 when unitedStatePhoneRegex.IsMatch var1 -> Ok()
    | _ -> Error "must either have exactly 10 digits or a '1' followed by 10 digits"