module TrainingRequestEndpoints

open DbLayer
open ExternalDependencies
open Falco
open GenericModels
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
        DescriptionOfNeeds: string;
    }

type TrainingRequestForm =
    {
        CaretakerType: CaretakerType;
        CaretakerFirstName: Option<string>;
        CaretakerLastName: Option<string>;
        CaretakerCompanyName: Option<string>;
        Email: string;
        Phone: string;
        SquirrelName: string;
        DescriptionOfNeeds: string;
    }

[<Literal>]
let requiredMessage = "is required"

let validateCompanyName form =
    match form with
        | { CaretakerType = CaretakerType.Person } -> Ok()
        | { CaretakerCompanyName = None } -> Error requiredMessage
        | { CaretakerCompanyName = Some("") } -> Error requiredMessage
        | _ -> Ok()

let validateFirstName form =
    match form with
        | { CaretakerType = CaretakerType.Company } -> Ok()
        | { CaretakerFirstName = None } -> Error requiredMessage
        | { CaretakerFirstName = Some("") } -> Error requiredMessage
        | _ -> Ok()

let validateLastName form =
    match form with
        | { CaretakerType = CaretakerType.Company } -> Ok()
        | { CaretakerLastName = None } -> Error requiredMessage
        | { CaretakerLastName = Some("") } -> Error requiredMessage
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

let validateForm (form : TrainingRequestForm) : Result<TrainingRequestForm, GenericModelResponse<TrainingRequestValidation>> =
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
                DescriptionOfNeeds = form.DescriptionOfNeeds
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
                    DescriptionOfNeeds = ""
                }
            }

let insertRequestToDatabase (form : TrainingRequestForm) (env : IGetDb) =
    task {
        let db = env.GetDb()
        use! shared = db.OpenContextAsync()
        try
            insertTask shared {
                for s in Database.main.TrainingRequest do
                entity {
                    TrainingRequestId = 1;
                    SquirrelName = form.SquirrelName;
                    OrganizationName = form.CaretakerCompanyName;
                    OwnerFirstName = form.CaretakerFirstName;
                    OwnerLastName = form.CaretakerLastName;
                    Email = form.Email;
                    Phone = Some form.Phone;
                    DescriptionOfNeeds = Some form.DescriptionOfNeeds;
                    SquirrelId = None;
                    OnboardUsername = None;
                    OnboardingDateTime = None;
                }
                getId s.TrainingRequestId
            } |> ignore
            return Ok {
                IsSuccess = true
                IsInternalError = false
                ValidationFailures = None
            }            
        with
        | ex ->
            printfn "SQL: %O" ex
            return Error {
                IsSuccess = false
                IsInternalError = true
                ValidationFailures = None
            }
    }            

let insertRequestToDatabaseOld (form : TrainingRequestForm) (env : IGetDb) =
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
                        entity { PersonId = 1; FirstName = form.CaretakerFirstName.Value; LastName = form.CaretakerLastName.Value }
                        getId p.PersonId
                    }
                | _ ->
                    insertTask shared {
                        for o in Database.main.Organization do
                        entity { OrganizationId = 1; Name = form.CaretakerCompanyName.Value }
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
        let caretakerTypeEnum = enum<CaretakerType> caretakerTypeInt
        let dataToValidate : TrainingRequestForm =
            {
                CaretakerType = caretakerTypeEnum
                CaretakerCompanyName = match caretakerTypeEnum with | CaretakerType.Company -> Some(form.GetString ("caretakerCompanyName", "")) | _ -> None
                CaretakerFirstName = match caretakerTypeEnum with | CaretakerType.Person -> Some(form.GetString ("caretakerFirstName", "")) | _ -> None
                CaretakerLastName = match caretakerTypeEnum with | CaretakerType.Person -> Some(form.GetString ("caretakerLastName", "")) | _ -> None
                Email = form.GetString ("email", "")
                Phone = form.GetString ("phone", "")
                SquirrelName = form.GetString ("squirrelname", "")
                DescriptionOfNeeds = form.GetString ("descriptionOfNeeds", "")
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

let getTrainingRequestsFromDb (env : IGetDb) (skipNumber : int) (length : int) =
    task {
        let db = env.GetDb()
        try
            let! requests =
                selectTask db {
                    for s in Database.main.TrainingRequest do
                    where (s.SquirrelId = None)
                    select (
                        s.SquirrelName,
                        s.Phone,
                        s.Email,
                        s.OwnerLastName,
                        s.OwnerFirstName,
                        s.OrganizationName,
                        s.DescriptionOfNeeds
                    ) into selected
                    skip skipNumber
                    take length
                    mapSeq (
                        let squirrelName, phoneNumber, email, firstNameMaybe, lastNameMaybe, companyNameMaybe, descriptionOfNeedsMaybe = selected
                        let trainingRequestForm : TrainingRequestForm = {
                            CaretakerType =
                                match companyNameMaybe.IsNone || companyNameMaybe.Value.Length = 0 with
                                | true -> CaretakerType.Person
                                | false -> CaretakerType.Company
                            CaretakerCompanyName = companyNameMaybe
                            CaretakerFirstName = firstNameMaybe
                            CaretakerLastName = lastNameMaybe
                            Email = email
                            Phone = phoneNumber |> Option.defaultValue ""
                            SquirrelName = squirrelName
                            DescriptionOfNeeds = descriptionOfNeedsMaybe |> Option.defaultValue ""
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

let getTrainingRequestCount (env : IGetDb) =
    task {
        let db = env.GetDb()
        try
            let! requests =
                selectTask db {
                    for s in Database.main.TrainingRequest do
                    where (s.SquirrelId = None)
                    count
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

let getTrainingRequests (env : IGetDb) =
    Auth.processAuthenticatedRequest
        (fun ctx ->
            task {
                let page = Math.Max(1, (Request.getQuery ctx).GetInt("page"))
                let pageLength = Math.Max(10, (Request.getQuery ctx).GetInt("length"))
                let skipCount = (page - 1) * pageLength
                let! existingTrainingRequests = getTrainingRequestsFromDb env skipCount pageLength
                let! recordCountResult = getTrainingRequestCount env
                let recordCount =
                    match recordCountResult with
                        | Ok foundCount -> foundCount
                        | Error _ -> pageLength + 1
                let jsonResponse =
                    match existingTrainingRequests with
                    | Ok foundList ->
                        let payload : PagedData<TrainingRequestForm> = {
                            Page = page;
                            TotalRecords = Some recordCount;
                            MorePages = recordCount > page * pageLength;
                            Data = (foundList |> Seq.truncate pageLength);
                        }
                        Response.withStatusCode 200 >> Response.ofJson payload
                    | Error errorResponse ->
                        Response.withStatusCode 500 >> Response.ofJson errorResponse
                return! jsonResponse ctx
            }
        )