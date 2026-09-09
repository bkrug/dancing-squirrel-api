module Calendar.Endpoints

open DbLayer.Database.main
open Falco
open GenericModels
open System
open System.Threading.Tasks
open Calendar.Models
open Calendar.Queries
open ValidationStandards

//Todo: Move into named constant
let roles = [TeacherRole]

let getParsedDayOfWeek (dayOfWeekOption: Option<string>) =
    match dayOfWeekOption with
    | None -> Error requiredMessage
    | Some "" -> Error requiredMessage
    | Some dayOfWeekString ->
        match Enum.TryParse<DayOfWeek> (dayOfWeekString, true) with
        | true, dayValue -> Ok(int64 dayValue)
        | _ -> Error "Must be Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, or Sunday"

let getParsedTimeOfDay (timeOfDayOption: Option<string>) =
    match timeOfDayOption with
    | None -> Error requiredMessage
    | Some "" -> Error requiredMessage
    | Some timeString ->
        match TimeOnly.TryParse timeString with
        | true, parsedTime ->
            Ok (int64(parsedTime.ToTimeSpan().TotalSeconds))
        | _ ->
            Error "Must be in the format 'hh:mm'"

let private emptyRowValidation : DefaultDayAvailabilityValidation =
    { TeacherId = ""; DayOfWeek = ""; StartTime = ""; EndTime = "" }

let private parseRow (row: CreateEditDefaultDayAvailability) : Result<DefaultAvailability, DefaultDayAvailabilityValidation> =
    let dayOfWeekR = getParsedDayOfWeek row.DayOfWeek
    let startTimeR = getParsedTimeOfDay row.StartTime
    let endTimeR = getParsedTimeOfDay row.EndTime
    match dayOfWeekR, startTimeR, endTimeR with
    | Ok dayOfWeek, Ok startTime, Ok endTime ->
        Ok {
            TeacherId = row.TeacherId
            DayOfWeek = dayOfWeek
            StartTimeUnix = startTime
            EndTimeUnix = endTime
            DefaultAvailabilityId = 0
        }
    | _ ->
        Error {
            TeacherId = ""
            DayOfWeek = match dayOfWeekR with Error msg -> msg | _ -> ""
            StartTime = match startTimeR with Error msg -> msg | _ -> ""
            EndTime = match endTimeR with Error msg -> msg | _ -> ""
        }

let private rowsOverlap (a: DefaultAvailability) (b: DefaultAvailability) : bool =
    a.TeacherId = b.TeacherId
    && a.DayOfWeek = b.DayOfWeek
    && a.StartTimeUnix < b.EndTimeUnix
    && b.StartTimeUnix < a.EndTimeUnix

// Only successfully parsed rows can be validated further; a row already Error stays untouched.
// A row is checked against the rows accepted so far (in input order), so when two rows overlap
// it is the later row that is reported as the failure.
let private validateRow (rows: list<Result<DefaultAvailability, DefaultDayAvailabilityValidation>>) : list<Result<DefaultAvailability, DefaultDayAvailabilityValidation>> =
    let addRow (validRows, results) (row: Result<DefaultAvailability, DefaultDayAvailabilityValidation>) =
        match row with
        | Error _ -> (validRows, row :: results)
        | Ok parsedRow when parsedRow.StartTimeUnix >= parsedRow.EndTimeUnix ->
            let orderError = Error { emptyRowValidation with EndTime = "StartTime must precede EndTime" }
            (validRows, orderError :: results)
        | Ok parsedRow when validRows |> List.exists (rowsOverlap parsedRow) ->
            let overlapError = Error { emptyRowValidation with StartTime = "overlaps another availability period" }
            (validRows, overlapError :: results)
        | Ok parsedRow ->
            (parsedRow :: validRows, row :: results)
    rows
    |> List.fold addRow ([], [])
    |> snd
    |> List.rev

// Every row is validated independently, so a single invalid row must not hide the others: on
// failure we return one validation entry per input row (index-aligned), padding the valid rows
// with an empty placeholder rather than collapsing the list down to just the failures.
let private combineRowResults (rows: list<Result<DefaultAvailability, DefaultDayAvailabilityValidation>>) : Result<list<DefaultAvailability>, DefaultAvailabilityValidation> =
    let hasErrors = rows |> List.exists (function Error _ -> true | Ok _ -> false)
    if hasErrors then
        rows
        |> List.map (function Error validation -> validation | Ok _ -> emptyRowValidation)
        |> List.toArray
        |> fun validations -> Error { Availabilities = validations }
    else
        rows
        |> List.choose (function Ok row -> Some row | Error _ -> None)
        |> Ok

let createDefaultAvailabilityFromForm
    (form: CreateEditDefaultAvailability)
    (upsertRecords: list<DefaultAvailability> -> Task<Result<list<DefaultAvailability>, RecordInsertError>>) =
    task {
        match form.Availabilities |> Array.toList |> List.map parseRow |> validateRow |> combineRowResults with
        | Error validation ->
            return Error (getGenericValidationFailure validation)
        | Ok parsedData ->
            let! dbResult =
                upsertRecords parsedData
                |> TaskResult.mapError getRecordInsertErrorResponse
            return dbResult
    }

let createDefaultAvailability (queries: ICalendarQueries) : HttpHandler =
    Auth.processAuthorizedRequest roles
        (fun ctx ->
            task {
                let! json = Request.getJson<CreateEditDefaultAvailability> ctx
                let! submissionResult = createDefaultAvailabilityFromForm json queries.UpsertDefaultAvailability
                return! getFormCreateResponse submissionResult ctx
            }
        )

let editDefaultAvailabilityFromForm
    (form: CreateEditDefaultAvailability)
    (upsertRecord: DefaultAvailability -> Task<Result<DefaultAvailability, RecordInsertError>>):
    CreateEditDefaultAvailability -> (DefaultAvailability -> Task<Result<DefaultAvailability, RecordInsertError>>) -> Result<bool, GenericModelResponse<DefaultAvailabilityValidation>> =
    failwith "Not implemented"

let editDefaultAvailability (queries: ICalendarQueries) : HttpHandler =
    Auth.processAuthorizedRequest roles
        (fun ctx ->
            task {
                let submissionResult = Ok getGenericSuccess
                let httpFormResponse = getFormEditResponse submissionResult
                return! httpFormResponse ctx
            }
        )

let createRecurringEvent (queries: ICalendarQueries) : HttpHandler =
    Auth.processAuthorizedRequest roles
        (fun ctx ->
            task {
                let submissionResult = Ok getGenericSuccess
                let httpFormResponse = getFormCreateResponse submissionResult
                return! httpFormResponse ctx
            }
        )

let editRecurringEvent (queries: ICalendarQueries) : HttpHandler =
    Auth.processAuthorizedRequest roles
        (fun ctx ->
            task {
                let submissionResult = Ok getGenericSuccess
                let httpFormResponse = getFormEditResponse submissionResult
                return! httpFormResponse ctx
            }
        )

let createSingleEvent (queries: ICalendarQueries) : HttpHandler =
    Auth.processAuthorizedRequest roles
        (fun ctx ->
            task {
                let submissionResult = Ok getGenericSuccess
                let httpFormResponse = getFormCreateResponse submissionResult
                return! httpFormResponse ctx
            }
        )

let editSingleEvent (queries: ICalendarQueries) : HttpHandler =
    Auth.processAuthorizedRequest roles
        (fun ctx ->
            task {
                let submissionResult = Ok getGenericSuccess
                let httpFormResponse = getFormEditResponse submissionResult
                return! httpFormResponse ctx
            }
        )
