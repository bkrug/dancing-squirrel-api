module TrainingRequest.Endpoints

open ExternalDependencies
open Falco
open GenericModels
open System
open System.Collections.Generic
open System.Text.RegularExpressions
open TrainingRequest.Models
open TrainingRequest.Queries
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies

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
        let! submissionResult =
            Ok dataToValidate
            |> Result.bind validateForm
            |> TaskResult.bindToTask insertRequestToConfiguredDb
        let httpFormResponse = getHttpFormResponse submissionResult
        return! httpFormResponse ctx
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
                let httpPagedResponse = getHttpPagedDataResponse existingTrainingRequests recordCountResult page pageLength
                return! httpPagedResponse ctx
            }
        )

let getSingleTrainingRequest (env: IGetDb) =
    Auth.processAuthenticatedRequest
        (fun ctx ->
            task {
                let trainingRequestId = Math.Max(0, (Request.getRoute ctx).GetInt("trainingRequestId"))
                let! existingTrainingRequest = getSingleTrainingRequestFromDb env trainingRequestId
                let httpRecordResponse = getHttpRecordResponse existingTrainingRequest
                return! httpRecordResponse ctx
            }
        )

let onboardClient (env : IGetDb) =
    Auth.processAuthenticatedRequest
        (fun ctx ->
            task {
                //SUSPECT: Authenticating a second time just so I can get the username
                let! authenticateResult = ctx.AuthenticateAsync CookieAuthenticationDefaults.AuthenticationScheme
                let username = authenticateResult.Principal.Identity.Name
                let trainingRequestId = (Request.getRoute ctx).GetInt "trainingRequestId"
                let! onboardingResult =
                    getSingleTrainingRequestFromDb env trainingRequestId
                    |> TaskResult.bind (onboardClientInDb env username)
                let httpFormResponse = getHttpFormResponse onboardingResult
                return! httpFormResponse ctx
            }
        )