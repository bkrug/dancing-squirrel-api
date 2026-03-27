module TrainingRequest

open System
open System.Text.RegularExpressions
open DbLayer
open Falco
open SqlHydra.Query
open System.Threading.Tasks

let connStr = "Data Source=/home/bkrug/Repos/dancing-squirrel-api/Database/DancingSquirrel.db;"
let db = Database.QueryContextFactory.Create(connStr, printfn "SQL: %O")

type TrainingRequestForm =
    {
        IsPerson: bool
        CaretakerName: string
        Email: string
        Phone: string
        SquirrelName: string
    }

type TrainingRequestValidation =
    {
        CaretakerType: string;
        CaretakerName: string;
        Email: string;
        Phone: string;
        SquirrelName: string;
    }

type TrainingRequestResponse =
    {
        IsSuccess: bool;
        IsInternalError: bool;
        ValidationFailures: Option<TrainingRequestValidation>;
    }

type CaretakerType =
    | Person = 1
    | Company = 2

[<Literal>]
let requiredMessage = "is required"

let validateRequiredName nameValue =
    match nameValue with
        | "" -> Error requiredMessage
        | _ -> Ok()

let emailRegex = Regex(@"^[\w\-\.]+@([\w-]+\.)+[\w-]{2,}$")
let validateEmail (value : string) =
    match value with
    | var1 when emailRegex.IsMatch var1 -> Ok()
    | "" -> Error requiredMessage
    | _ -> Error "must be an email address"

// Must have exactly 10 digits, or a 1 followed by exactly 10 digits.
// Non-digits are accepted and ignored.
let unitedStatePhoneRegex = Regex(@"^1?([^\d]*\d){10}[^\d]*$")
let containsLetterRegex = Regex(@"[a-zA-Z]+")
let validatePhone (value : string) =
    match value with
    | "" -> Ok()
    | var1 when containsLetterRegex.IsMatch var1 -> Error "must not contain letters"
    | var1 when unitedStatePhoneRegex.IsMatch var1 -> Ok()
    | _ -> Error "must either have exactly 10 digits or a '1' followed by 10 digits"

let removeNonDigits givenString =
    Seq.toList givenString
    |> Seq.filter (fun c -> Char.IsDigit c)
    |> Seq.toArray
    |> String

let validateForm (form : TrainingRequestForm) =
    let validationResults =
        [
            validateRequiredName form.CaretakerName;
            validateEmail form.Email;
            validatePhone form.Phone;
            validateRequiredName form.SquirrelName;
        ]
    let failureCount = Seq.filter (fun (kvp : Result<unit, string>) -> kvp.IsError) validationResults |> Seq.length
    match failureCount with
        | 0 -> Ok {
                IsPerson = form.IsPerson
                CaretakerName = form.CaretakerName
                Email = form.Email
                Phone = removeNonDigits form.Phone
                SquirrelName = form.SquirrelName
            }
        | _ -> Error {
                IsSuccess = false
                IsInternalError = false
                ValidationFailures = Some {
                    CaretakerType = ""
                    CaretakerName = match validationResults[0] with | Error msg -> msg | _ -> ""
                    Email = match validationResults[1] with | Error msg -> msg | _ -> ""
                    Phone = match validationResults[2] with | Error msg -> msg | _ -> ""
                    SquirrelName = match validationResults[3] with | Error msg -> msg | _ -> ""
                }
            }

let insertRequestToDatabase (form : TrainingRequestForm) =
    task {
        use! shared = db.OpenContextAsync()
        shared.BeginTransaction()
        try
            let! personOrOrganizationId =
                match form.IsPerson with
                | true ->
                    insertTask shared {
                        for p in Database.main.Person do
                        entity { PersonId = 1; FirstName = "n/a"; LastName = form.CaretakerName }
                        getId p.PersonId
                    }
                | false ->
                    insertTask shared {
                        for o in Database.main.Organization do
                        entity { OrganizationId = 1; Name = form.CaretakerName }
                        getId o.OrganizationId
                    }
            let! ownerId =
                insertTask shared {
                    for so in Database.main.SquirrelOwner do
                    entity {
                        SquirrelOwnerId = 0;
                        PersonId = if form.IsPerson then Some personOrOrganizationId else None;
                        OrganizationId = if form.IsPerson then None else Some personOrOrganizationId;
                        PhoneNumber = Some form.Phone;
                        Email = Some form.Email;
                    }
                    getId so.SquirrelOwnerId
                }
            let! squirrelId =
                insertTask shared {
                    for s in Database.main.Squirrel do
                    entity { SquirrelId = 0; Name = form.SquirrelName; SquirrelOwnerId = ownerId }
                    getId s.SquirrelId
                }
            shared.CommitTransaction()
            return Ok {
                IsSuccess = true
                IsInternalError = false
                ValidationFailures = None
            }            
        with
        | ex ->
            shared.RollbackTransaction()
            printfn "SQL: %O" ex
            return Error {
                IsSuccess = false
                IsInternalError = true
                ValidationFailures = None
            }
    }

let createTrainingRequest : HttpHandler = fun ctx ->
    task {
        let! form = Request.getForm ctx
        let typeFromForm = form.GetInt("caretakertype", 0)
        let enumCastAsInt = int CaretakerType.Person
        let dataToValidate =
            {
                IsPerson = typeFromForm = enumCastAsInt
                CaretakerName = form.GetString ("caretakername", "")
                Email = form.GetString ("email", "")
                Phone = form.GetString ("phone", "")
                SquirrelName = form.GetString ("squirrelname", "")
            }
        let! resultOfChain =
            Ok dataToValidate
            |> Result.bind validateForm
            |> TaskResult.bindToTask insertRequestToDatabase
        let jsonResponse =
            match resultOfChain with
            | Ok trainingRequestResponse ->
                Response.withStatusCode 201 >> Response.ofJson trainingRequestResponse
            | Error trainingRequestResponse when trainingRequestResponse.ValidationFailures.IsSome ->
                Response.withStatusCode 400 >> Response.ofJson trainingRequestResponse
            | Error trainingRequestResponse ->
                Response.withStatusCode 500 >> Response.ofJson trainingRequestResponse
        return! jsonResponse ctx
    }