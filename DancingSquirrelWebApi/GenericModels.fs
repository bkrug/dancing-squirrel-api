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