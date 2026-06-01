module TrainingRequest.Endpoints

open DbLayer.Database
open Falco
open GenericModels
open System
open System.Collections.Generic
open System.Text.Json
open System.Text.RegularExpressions
open System.Threading.Tasks
open TrainingRequest.Models
open TrainingRequest.Queries
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open ValidationStandards

let private roles = [OnboarderRole]

//***
// Validation Methods
//***

let private validateCompanyName form =
    match form with
        | { CaretakerType = CaretakerType.Person } -> Ok()
        | { CaretakerCompanyName = None } -> Error requiredMessage
        | { CaretakerCompanyName = Some("") } -> Error requiredMessage
        | _ -> Ok()

let private validateFirstName form =
    match form with
        | { CaretakerType = CaretakerType.Company } -> Ok()
        | { CaretakerFirstName = None } -> Error requiredMessage
        | { CaretakerFirstName = Some("") } -> Error requiredMessage
        | _ -> Ok()

let private validateLastName form =
    match form with
        | { CaretakerType = CaretakerType.Company } -> Ok()
        | { CaretakerLastName = None } -> Error requiredMessage
        | { CaretakerLastName = Some("") } -> Error requiredMessage
        | _ -> Ok()        

let private removeNonDigits givenString =
    Seq.toList givenString
    |> Seq.filter (fun c -> Char.IsDigit c)
    |> Seq.toArray
    |> String

let private getValidationMessage keyName (validationResults: IDictionary<string,Result<unit,string>>) =
    match validationResults[keyName] with | Error msg -> msg | _ -> ""

let private validateForm (form : TrainingRequestForm) : Result<TrainingRequestForm, GenericModelResponse<TrainingRequestValidation>> =
    let validationResults =
        dict [
            nameof form.CaretakerCompanyName,   validateCompanyName form;
            nameof form.CaretakerFirstName,     validateFirstName form;
            nameof form.CaretakerLastName,      validateLastName form;
            nameof form.Email,                  validateEmailField form.Email;
            nameof form.Phone,                  validatePhoneField form.Phone;
            nameof form.SquirrelName,           validateRequiredField form.SquirrelName;
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
                IsNotFoundError = true
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

//***
// Endpoint methods
//***

let createTrainingRequestFromForm (form: FormData) (insertRec:TrainingRequestFormInserter<'a>) =
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
    let! submissionResult =
        Ok dataToValidate
        |> Result.bind validateForm
        |> TaskResult.bindToTask insertRec
    submissionResult

let createTrainingRequest (queries: ITrainingRequestQueries) : HttpHandler = fun ctx ->
    task {
        let! form = Request.getForm ctx
        let! submissionResult = createTrainingRequestFromForm form queries.InsertTrainingRequest
        let httpFormResponse = getFormCreateResponse submissionResult
        return! httpFormResponse ctx
    }

let getTrainingRequests (queries: ITrainingRequestQueries) =
    Auth.processAuthorizedRequest roles
        (fun ctx ->
            task {
                let page = Math.Max(1, (Request.getQuery ctx).GetInt("page"))
                let pageLength = Math.Max(10, (Request.getQuery ctx).GetInt("length"))
                let skipCount = (page - 1) * pageLength
                let! existingTrainingRequests = queries.SelectMultiTrainingRequests skipCount pageLength
                let! recordCountResult = queries.CountTrainingRequests
                let httpPagedResponse = getHttpPagedDataResponse existingTrainingRequests recordCountResult page pageLength
                return! httpPagedResponse ctx
            }
        )

let getSingleTrainingRequest (queries: ITrainingRequestQueries) =
    Auth.processAuthorizedRequest roles
        (fun ctx ->
            task {
                let trainingRequestId = Math.Max(0, (Request.getRoute ctx).GetInt("trainingRequestId"))
                let! existingTrainingRequest = queries.SelectSingleTrainingRequest trainingRequestId
                let httpRecordResponse = getHttpRecordResponse existingTrainingRequest
                return! httpRecordResponse ctx
            }
        )

let validatedOnboardingRequest (trainingRequest : main.TrainingRequest) =
    let res =
        match trainingRequest.SquirrelId with
        | None -> Ok trainingRequest
        | _ -> Error (getGenericValidationFailure "Caretaker and Squirrel have already been onboarded")
    Task.FromResult res

let onboardClient (queries: ITrainingRequestQueries) =
    Auth.processAuthorizedRequest roles
        (fun ctx ->
            task {
                //SUSPECT: Authenticating a second time just so I can get the username
                let! authenticateResult = ctx.AuthenticateAsync CookieAuthenticationDefaults.AuthenticationScheme
                let username = authenticateResult.Principal.Identity.Name
                let trainingRequestId = (Request.getRoute ctx).GetInt "trainingRequestId"
                let! onboardingRequestJson = Request.getBodyString ctx
                let onboardingRequestObject = JsonSerializer.Deserialize<OnboardingRequest>(onboardingRequestJson, defaultJsonOptions)
                let! onboardingResult =
                    queries.SelectSingleTrainingRequest trainingRequestId
                    |> TaskResult.bind validatedOnboardingRequest
                    |> TaskResult.bind (queries.InsertOnboardedClient username onboardingRequestObject)
                let httpFormResponse = getFormCreateResponse onboardingResult
                return! httpFormResponse ctx
            }
        )