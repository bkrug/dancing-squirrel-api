module TrainingRequest

open System
open System.Text.RegularExpressions
open DbLayer
open Falco
open SqlHydra.Query

let connStr = "Data Source=/home/bkrug/Repos/dancing-squirrel-api/Database/DancingSquirrel.db;"
let db = Database.QueryContextFactory.Create(connStr, printfn "SQL: %O")

let getPeople id =
    selectTask db {
        for p in Database.main.Person do
        where (p.PersonId = id)
        select p
    }

type trainingRequest =
    {
        IsPerson: bool
        CaretakerName: string
        Email: string
        Phone: string
        SquirrelName: string
    }

type trainingRequestValidation =
    {
        CaretakerType: string;
        CaretakerName: string;
        Email: string;
        Phone: string;
        SquirrelName: string;
    }

type trainingRequestResponse =
    {
        OwnerId: int64
        SquirrelId: int64
    }

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

let validateForm (form : trainingRequest) : Result<trainingRequest, trainingRequestValidation> =
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
                CaretakerType = ""
                CaretakerName = match validationResults[0] with | Error msg -> msg | _ -> ""
                Email = match validationResults[1] with | Error msg -> msg | _ -> ""
                Phone = match validationResults[2] with | Error msg -> msg | _ -> ""
                SquirrelName = match validationResults[3] with | Error msg -> msg | _ -> ""
            }

let insertTrainingRequest trainingRequestModel =
    task {
        use! shared = db.OpenContextAsync()
        shared.BeginTransaction()
        let! personOrOrganizationId =
            match trainingRequestModel.IsPerson with
            | true ->
                insertTask shared {
                    for p in Database.main.Person do
                    entity { PersonId = 1; FirstName = "n/a"; LastName = trainingRequestModel.CaretakerName }
                    getId p.PersonId
                }
            | false ->
                insertTask shared {
                    for o in Database.main.Organization do
                    entity { OrganizationId = 1; Name = trainingRequestModel.CaretakerName }
                    getId o.OrganizationId
                }
        let! ownerId =
            insertTask shared {
                for so in Database.main.SquirrelOwner do
                entity {
                    SquirrelOwnerId = 0;
                    PersonId = if trainingRequestModel.IsPerson then Some personOrOrganizationId else None;
                    OrganizationId = if trainingRequestModel.IsPerson then None else Some personOrOrganizationId;
                    PhoneNumber = Some trainingRequestModel.Phone;
                    Email = Some trainingRequestModel.Email;
                }
                getId so.SquirrelOwnerId
            }
        let! squirrelId =
            insertTask shared {
                for s in Database.main.Squirrel do
                entity { SquirrelId = 0; Name = trainingRequestModel.SquirrelName; SquirrelOwnerId = ownerId }
                getId s.SquirrelId
            }
        shared.CommitTransaction()
        return
            {
                OwnerId = ownerId
                SquirrelId = squirrelId
            }
    }

let createTrainingRequest : HttpHandler = fun ctx ->
    task {
        let! form = Request.getForm ctx
        let dataToValidate =
            {
                IsPerson = form.GetString("caretakertype", "") = "person"
                CaretakerName = form.GetString ("caretakername", "")
                Email = form.GetString ("email", "")
                Phone = form.GetString ("phone", "")
                SquirrelName = form.GetString ("squirrelname", "")
            }
        let jsonResponse =
            match validateForm dataToValidate with
            | Ok dataToSave ->
                let! insertionResult = insertTrainingRequest dataToSave
                Response.withStatusCode 200 >> Response.ofJson insertionResult
            | Error validationFailure ->
                Response.withStatusCode 400 >> Response.ofJson validationFailure
        return! jsonResponse ctx
    }