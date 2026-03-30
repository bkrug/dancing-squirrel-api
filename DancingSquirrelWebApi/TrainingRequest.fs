module TrainingRequest

open System
open System.Text.RegularExpressions
open DbEnv
open DbLayer
open Falco
open SqlHydra.Query

type TrainingRequestForm =
    {
        IsPerson: bool
        CaretakerFirstName: string;
        CaretakerLastName: string;
        CaretakerCompanyName: string
        Email: string
        Phone: string
        SquirrelName: string
    }

type TrainingRequestValidation =
    {
        CaretakerType: string;
        CaretakerFirstName: string;
        CaretakerLastName: string;
        CaretakerCompanyName: string
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

let validateCompanyName form =
    match form with
        | { IsPerson = true } -> Ok()
        | { CaretakerCompanyName = "" } -> Error requiredMessage
        | _ -> Ok()

let validateFirstName form =
    match form with
        | { IsPerson = false } -> Ok()
        | { CaretakerFirstName = "" } -> Error requiredMessage
        | _ -> Ok()

let validateLastName form =
    match form with
        | { IsPerson = false } -> Ok()
        | { CaretakerLastName = "" } -> Error requiredMessage
        | _ -> Ok()        

let validateRequiredName nameValue =
    match nameValue with
        | "" -> Error requiredMessage
        | _ -> Ok()

let emailRegex = Regex @"^[\w\-\.]+@([\w-]+\.)+[\w-]{2,}$"
let validateEmail (value : string) =
    match value with
    | var1 when emailRegex.IsMatch var1 -> Ok()
    | "" -> Error requiredMessage
    | _ -> Error "must be an email address"

// Must have exactly 10 digits, or a 1 followed by exactly 10 digits.
// Non-digits are accepted and ignored.
let unitedStatePhoneRegex = Regex @"^1?([^\d]*\d){10}[^\d]*$"
let containsLetterRegex = Regex @"[a-zA-Z]+"
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
            validateCompanyName form;
            validateFirstName form;
            validateLastName form;
            validateEmail form.Email;
            validatePhone form.Phone;
            validateRequiredName form.SquirrelName;
        ]
    let failureCount = Seq.filter (fun (kvp : Result<unit, string>) -> kvp.IsError) validationResults |> Seq.length
    match failureCount with
        | 0 -> Ok {
                IsPerson = form.IsPerson
                CaretakerCompanyName = form.CaretakerCompanyName
                CaretakerFirstName = form.CaretakerFirstName
                CaretakerLastName = form.CaretakerLastName
                Email = form.Email
                Phone = removeNonDigits form.Phone
                SquirrelName = form.SquirrelName
            }
        | _ -> Error {
                IsSuccess = false
                IsInternalError = false
                ValidationFailures = Some {
                    CaretakerType = ""
                    CaretakerCompanyName = match validationResults[0] with | Error msg -> msg | _ -> ""
                    CaretakerFirstName = match validationResults[1] with | Error msg -> msg | _ -> ""
                    CaretakerLastName = match validationResults[2] with | Error msg -> msg | _ -> ""
                    Email = match validationResults[3] with | Error msg -> msg | _ -> ""
                    Phone = match validationResults[4] with | Error msg -> msg | _ -> ""
                    SquirrelName = match validationResults[5] with | Error msg -> msg | _ -> ""
                }
            }

let insertRequestToDatabase (form : TrainingRequestForm) (env : IGetDb) =
    task {
        let db = env.GetDb()
        use! shared = db.OpenContextAsync()
        shared.BeginTransaction()
        try
            let! personOrOrganizationId =
                match form.IsPerson with
                | true ->
                    insertTask shared {
                        for p in Database.main.Person do
                        entity { PersonId = 1; FirstName = form.CaretakerFirstName; LastName = form.CaretakerLastName }
                        getId p.PersonId
                    }
                | false ->
                    insertTask shared {
                        for o in Database.main.Organization do
                        entity { OrganizationId = 1; Name = form.CaretakerCompanyName }
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
            insertTask shared {
                for s in Database.main.Squirrel do
                entity { SquirrelId = 0; Name = form.SquirrelName; SquirrelOwnerId = ownerId }
                getId s.SquirrelId
            } |> ignore
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

let createTrainingRequest (env : IGetDb) : HttpHandler = fun ctx ->
    task {
        let! form = Request.getForm ctx
        let typeFromForm = form.GetInt("caretakertype", 0)
        let enumCastAsInt = int CaretakerType.Person
        let dataToValidate =
            {
                IsPerson = typeFromForm = enumCastAsInt
                CaretakerCompanyName = form.GetString ("caretakerCompanyName", "")
                CaretakerFirstName = form.GetString ("caretakerFirstName", "")
                CaretakerLastName = form.GetString ("caretakerLastName", "")
                Email = form.GetString ("email", "")
                Phone = form.GetString ("phone", "")
                SquirrelName = form.GetString ("squirrelname", "")
            }
        let insertRequestToConfiguredDb (form : TrainingRequestForm) = insertRequestToDatabase form env
        let! resultOfChain =
            Ok dataToValidate
            |> Result.bind validateForm
            |> TaskResult.bindToTask insertRequestToConfiguredDb
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