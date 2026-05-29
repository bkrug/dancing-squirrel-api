module GenericModels

open Falco
open System.Text.Json

type PagedData<'TValue> =
    {
        Page: int64;
        TotalRecords: int64;
        Data: seq<'TValue>;
    }

type GenericModelResponse<'TValue> =
    {
        IsSuccess: bool;
        IsInternalError: bool;
        IsNotFoundError: bool;
        ValidationFailures: Option<'TValue>;
    }

type RecordRetrievalErrors =
    | DbAccessError = 1
    | NotFound = 2
    | ExpectedSingleFoundMultiple = 3

let internalErrorResponse =
    {
        IsSuccess = false
        IsInternalError = true
        IsNotFoundError = false
        ValidationFailures = None
    }

//This is considered an internal error, because this error is only expected in situations where we forgot to put a unique key on some lookup field.
let foundMultipleRecordsResponse =
    {
        IsSuccess = false
        IsInternalError = true
        IsNotFoundError = false
        ValidationFailures = Some "Expected single record, but found multiple"
    }

//This is not considered an internal error. The caller probably just supplied an invalid key.
let notFoundResponse =
    {
        IsSuccess = false
        IsInternalError = false
        IsNotFoundError = true
        ValidationFailures = Some "Not found"
    }

let getGenericSuccess =
    {
        IsSuccess = true
        IsInternalError = false
        IsNotFoundError = false
        ValidationFailures = None
    }

let getGenericValidationFailure vFailure =
    {
        IsSuccess = false
        IsInternalError = false
        IsNotFoundError = false
        ValidationFailures = Some vFailure
    }

let getFormResponse successCode formSubmissionResult =
    match formSubmissionResult with
    | Error failureResponse when failureResponse.IsInternalError ->
        Response.withStatusCode 500 >> Response.ofJson failureResponse
    | Error failureResponse when failureResponse.IsNotFoundError ->
        Response.withStatusCode 404 >> Response.ofJson failureResponse
    | Error failureResponse ->
        Response.withStatusCode 400 >> Response.ofJson failureResponse
    | Ok successResponse ->
        Response.withStatusCode successCode >> Response.ofJson successResponse  

let getFormCreateResponse formSubmissionResult = getFormResponse 201 formSubmissionResult

let getFormEditResponse formSubmissionResult = getFormResponse 200 formSubmissionResult

let getHttpPagedDataResponse recordSequenceResult recordCountResult page pageLength =
    match recordSequenceResult with
    | Ok foundSequence ->
        let recordCount =
            match recordCountResult with
            | Ok foundCount -> foundCount
            | Error _ -> (page - 1) * pageLength + (foundSequence |> Seq.length)
        let payload = {
            Page = page;
            TotalRecords = recordCount;
            Data = (foundSequence |> Seq.truncate pageLength);
        }
        Response.withStatusCode 200 >> Response.ofJson payload
    | Error _ ->
        Response.withStatusCode 500 >> Response.ofJson internalErrorResponse

let getHttpRecordResponse recordLookupResult =
    match recordLookupResult with
    | Error errorResponse when errorResponse.IsInternalError->
        Response.withStatusCode 500 >> Response.ofJson errorResponse
    | Error failureResponse when failureResponse.IsNotFoundError ->
        Response.withStatusCode 404 >> Response.ofJson failureResponse
    | Error errorResponse ->
        Response.withStatusCode 400 >> Response.ofJson errorResponse
    | Ok foundRecord ->
        Response.withStatusCode 200 >> Response.ofJson foundRecord

let defaultJsonOptions =
    let options : JsonSerializerOptions = JsonSerializerOptions()
    options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
    options

let AdminRole = "Admin"
let OnboarderRole = "Onboarder"
type Roles = AdminRole | OnboarderRole