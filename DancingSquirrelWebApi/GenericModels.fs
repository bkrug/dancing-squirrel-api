module GenericModels

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

// I'm not sure about this "RecordRetrievalErrors" enum yet.
// It prevents us from piping methods together the way we do with "insertRequestToDatabase", which returns a "GenericModelResponse".
// In the long run, I might just write several variables the resemble "internalErrorResponse".
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