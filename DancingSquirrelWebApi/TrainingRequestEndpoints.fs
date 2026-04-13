module TrainingRequestEndpoints

open DbLayer
open ExternalDependencies
open Falco
open SqlHydra.Query
open System
open System.Collections.Generic
open System.Text.RegularExpressions

type CaretakerType =
    | Person = 1
    | Company = 2

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

type TrainingRequestForm =
    {
        CaretakerType: CaretakerType;
        CaretakerFirstName: string;
        CaretakerLastName: string;
        CaretakerCompanyName: string
        Email: string
        Phone: string
        SquirrelName: string
    }

type TrainingRequestResponse =
    {
        IsSuccess: bool;
        IsInternalError: bool;
        ValidationFailures: Option<TrainingRequestValidation>;
    }

type PagedData<'TValue> =
    {
        Page: int64;
        MorePages: bool;
        TotalRecords: Option<int64>;
        Data: seq<'TValue>;
    }

[<Literal>]
let requiredMessage = "is required"

let validateCompanyName form =
    match form with
        | { CaretakerType = CaretakerType.Person } -> Ok()
        | { CaretakerCompanyName = "" } -> Error requiredMessage
        | _ -> Ok()

let validateFirstName form =
    match form with
        | { CaretakerType = CaretakerType.Company } -> Ok()
        | { CaretakerFirstName = "" } -> Error requiredMessage
        | _ -> Ok()

let validateLastName form =
    match form with
        | { CaretakerType = CaretakerType.Company } -> Ok()
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

let getValidationMessage keyName (validationResults: IDictionary<string,Result<unit,string>>) =
    match validationResults[keyName] with | Error msg -> msg | _ -> ""

let validateForm (form : TrainingRequestForm) : Result<TrainingRequestForm, TrainingRequestResponse> =
    let validationResults =
        dict [
            nameof form.CaretakerCompanyName,   validateCompanyName form;
            nameof form.CaretakerFirstName,     validateFirstName form;
            nameof form.CaretakerLastName,      validateLastName form;
            nameof form.Email,                  validateEmail form.Email;
            nameof form.Phone,                  validatePhone form.Phone;
            nameof form.SquirrelName,           validateRequiredName form.SquirrelName;
        ]
    let failureCount = validationResults |> Seq.filter (fun kvp -> kvp.Value.IsError) |> Seq.length
    match failureCount with
        | 0 -> Ok {
                CaretakerType = form.CaretakerType
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
                    CaretakerCompanyName = getValidationMessage (nameof form.CaretakerCompanyName) validationResults
                    CaretakerFirstName = getValidationMessage (nameof form.CaretakerFirstName) validationResults
                    CaretakerLastName = getValidationMessage (nameof form.CaretakerLastName) validationResults
                    Email = getValidationMessage (nameof form.Email) validationResults
                    Phone = getValidationMessage (nameof form.Phone) validationResults
                    SquirrelName = getValidationMessage (nameof form.SquirrelName) validationResults
                }
            }

let insertRequestToDatabase (form : TrainingRequestForm) (env : IGetDb) =
    task {
        let db = env.GetDb()
        use! shared = db.OpenContextAsync()
        shared.BeginTransaction()
        try
            let! personOrOrganizationId =
                match form.CaretakerType with
                | CaretakerType.Person ->
                    insertTask shared {
                        for p in Database.main.Person do
                        entity { PersonId = 1; FirstName = form.CaretakerFirstName; LastName = form.CaretakerLastName }
                        getId p.PersonId
                    }
                | _ ->
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
                        PersonId = if form.CaretakerType = CaretakerType.Person then Some personOrOrganizationId else None;
                        OrganizationId = if form.CaretakerType = CaretakerType.Company then None else Some personOrOrganizationId;
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
        let caretakerTypeInt = form.GetInt("caretakertype", 0)
        let dataToValidate : TrainingRequestForm =
            {
                CaretakerType = enum<CaretakerType> caretakerTypeInt
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

let getTrainingRequestsFromDb (env : IGetDb) =
    task {
        let db = env.GetDb()
        try
            let! requests =
                selectTask db {
                    for s in Database.main.Squirrel do
                    join so in Database.main.SquirrelOwner on (s.SquirrelOwnerId = so.SquirrelOwnerId)
                    leftJoin o in Database.main.Organization on (so.OrganizationId.Value = o.Value.OrganizationId)
                    leftJoin p in Database.main.Person on (so.PersonId.Value = p.Value.PersonId)
                    select (
                        s.Name,
                        so.PhoneNumber,
                        so.Email,
                        so.PersonId,
                        p |> Option.map _.FirstName,
                        p |> Option.map _.LastName,
                        o |> Option.map _.Name
                    ) into selected
                    mapSeq (
                        let squirrelName, phoneNumber, email, personId, firstNameMaybe, lastNameMaybe, companyNameMaybe = selected
                        let trainingRequestForm : TrainingRequestForm = {
                            CaretakerType = match personId.IsSome with | true -> CaretakerType.Person | false -> CaretakerType.Company
                            CaretakerCompanyName = companyNameMaybe |> Option.defaultValue ""
                            CaretakerFirstName = firstNameMaybe |> Option.defaultValue ""
                            CaretakerLastName = lastNameMaybe |> Option.defaultValue ""
                            Email = email |> Option.defaultValue ""
                            Phone = phoneNumber |> Option.defaultValue ""
                            SquirrelName = squirrelName
                        }
                        trainingRequestForm
                    )
                }
            return Ok requests
        with
        | ex ->
            printfn "SQL: %O" ex
            return Error {
                IsSuccess = false
                IsInternalError = true
                ValidationFailures = None
            }
    }    

let getTrainingRequests (env : IGetDb) : HttpHandler = fun ctx ->
    task {
        let! existingTrainingRequests = getTrainingRequestsFromDb env
        let jsonResponse =
            match existingTrainingRequests with
            | Ok foundList ->
                let payload : PagedData<TrainingRequestForm> = {
                    Page = 1;
                    TotalRecords = None;
                    MorePages = false;
                    Data = foundList;
                }
                Response.withStatusCode 200 >> Response.ofJson payload
            | Error errorResponse ->
                Response.withStatusCode 500 >> Response.ofJson errorResponse
        return! jsonResponse ctx
    }