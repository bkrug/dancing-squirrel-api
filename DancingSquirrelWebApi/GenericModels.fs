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
        ValidationFailures = None
    }

//This is considered an internal error, because this error is only expected in situations where we forgot to put a unique key on some lookup field.
let foundMultipleRecordsResponse =
    {
        IsSuccess = false
        IsInternalError = true
        ValidationFailures = Some "Expected single record, but found multiple"
    }

//This is not considered an internal error. The caller probably just supplied an invalid key.
let notFoundResponse =
    {
        IsSuccess = false
        IsInternalError = false
        ValidationFailures = Some "Not found"
    }

let getHttpFormResponse formSubmissionResult =
    match formSubmissionResult with
    | Ok successResponse ->
        Response.withStatusCode 201 >> Response.ofJson successResponse
    | Error failureResponse when failureResponse.ValidationFailures.IsSome ->
        Response.withStatusCode 400 >> Response.ofJson failureResponse
    | Error failureResponse ->
        Response.withStatusCode 500 >> Response.ofJson failureResponse

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
    | Ok foundRecord ->
        Response.withStatusCode 200 >> Response.ofJson foundRecord
    | Error errorResponse when not errorResponse.IsInternalError ->
        Response.withStatusCode 400 >> Response.ofJson errorResponse
    | Error errorResponse ->
        Response.withStatusCode 500 >> Response.ofJson errorResponse

let defaultJsonOptions =
    let options : JsonSerializerOptions = JsonSerializerOptions()
    options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
    options