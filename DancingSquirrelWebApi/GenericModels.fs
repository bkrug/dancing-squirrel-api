module GenericModels

open Falco

type PagedData<'TValue> =
    {
        Page: int64;
        MorePages: bool;
        TotalRecords: Option<int64>;
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

type RecordLookupValidation =
    {
        LookupFailureMessage: string
        DbErrorType: RecordRetrievalErrors
    }

let internalErrorResponse =
    {
        IsSuccess = false
        IsInternalError = true
        ValidationFailures = None
    }

let foundMultipleRecordsResponse =
    {
        IsSuccess = false
        IsInternalError = true
        ValidationFailures = Some {
            LookupFailureMessage = "Expected single record, but found multiple"
            DbErrorType = RecordRetrievalErrors.ExpectedSingleFoundMultiple
        }
    }

let notFoundResponse =
    {
        IsSuccess = false
        IsInternalError = false
        ValidationFailures = Some {
            LookupFailureMessage = "Not found"
            DbErrorType = RecordRetrievalErrors.NotFound
        }
    }

let getHttpFormResponse formSubmissionResult =
    match formSubmissionResult with
    | Ok successResponse ->
        Response.withStatusCode 201 >> Response.ofJson successResponse
    | Error failureResponse when failureResponse.ValidationFailures.IsSome ->
        Response.withStatusCode 400 >> Response.ofJson failureResponse
    | Error failureResponse ->
        Response.withStatusCode 500 >> Response.ofJson failureResponse

let getHttpRecordResponse recordLookupResult =
    match recordLookupResult with
    | Ok foundRecord ->
        Response.withStatusCode 200 >> Response.ofJson foundRecord
    | Error errorResponse when not errorResponse.IsInternalError ->
        Response.withStatusCode 400 >> Response.ofJson errorResponse
    | Error errorResponse ->
        Response.withStatusCode 500 >> Response.ofJson errorResponse