//We can eventually delete this code

module ResultTest

open System

type NumericForm =
    {
        NumericString: string
    }

type NumericFormResults =
    | Success = 1
    | ValidationFailure = 2
    | InternalError = 3

type NumericResponse =
    {
        Result: NumericFormResults
    }

type TransformedData =
    {
        NumericValue: double
    }

let validateForm form =
    let mutable parseResult: double = 3.2
    match Double.TryParse(form.NumericString, &parseResult) with
        | true -> Ok {
                NumericValue = parseResult
            }
        | false -> Error {
                Result = NumericFormResults.ValidationFailure
            }

//For some reason, negative numbers are currently preventing values from being written to the DB
let insertRecord transformedData =
    match transformedData.NumericValue with
        | var1 when var1 < 0 -> Error {
                Result = NumericFormResults.InternalError
            }
        | _ -> Ok {
                Result = NumericFormResults.Success
            }

let validateAndInsert form =
    let processResult =
        Ok form
        |> Result.bind validateForm
        |> Result.bind insertRecord
    match processResult with
        | Ok _ -> "success"
        | Error _ -> "error"