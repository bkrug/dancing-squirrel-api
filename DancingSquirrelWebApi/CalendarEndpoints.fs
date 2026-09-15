module Calendar.Endpoints

open DbLayer.Database.main
open Falco
open GenericModels
open System.Threading.Tasks
open Calendar.Models
open Calendar.Queries
open ValidationStandards

let private parseRequiredString inputValue =
    match inputValue |> Extensions.getOptionFromLiar |> Extensions.emptyStringToNone with
    | Some nonEmpty -> Ok nonEmpty
    | None -> Error requiredMessage

let private parseDayOfWeek (dayOfWeek: string) =
    match System.Enum.TryParse<System.DayOfWeek> (dayOfWeek, true) with
    | true, dayValue -> Ok(int64 dayValue)
    | _ -> Error "Must be Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, or Sunday"

let private parseTimeOfDay (timeOfDay: string) =
    match System.TimeOnly.TryParse timeOfDay with
    | true, parsedTime -> Ok (int64(parsedTime.ToTimeSpan().TotalSeconds))
    | _ -> Error "Must be in the format 'hh:mm'"

let private emptyRowValidation : DefaultDayAvailabilityValidation =
    { DayOfWeek = ""; StartTime = ""; EndTime = "" }

let private parseRow (teacherId: int) (row: CreateEditDefaultDayAvailability) : Result<DefaultAvailability, DefaultDayAvailabilityValidation> =
    let dayOfWeekR = parseRequiredString row.DayOfWeek |> Result.bind parseDayOfWeek
    let startTimeR = parseRequiredString row.StartTime |> Result.bind parseTimeOfDay
    let endTimeR = parseRequiredString row.EndTime |> Result.bind parseTimeOfDay
    match dayOfWeekR, startTimeR, endTimeR with
    | Ok dayOfWeek, Ok startTime, Ok endTime ->
        Ok {
            TeacherId = teacherId
            DayOfWeek = dayOfWeek
            StartTimeUnix = startTime
            EndTimeUnix = endTime
            DefaultAvailabilityId = 0
        }
    | _ ->
        Error {
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

let crudDefaultAvailabilityFromForm
    (form: CreateEditDefaultAvailability)
    (loggedInTeacherId: int)
    (queries: ICalendarQueries) =
    task {
        match form.Availabilities |> Array.toList |> List.map (parseRow loggedInTeacherId) |> validateRow |> combineRowResults with
        | Error validation ->
            return Error (getGenericValidationFailure validation)
        | Ok parsedData ->
            do! queries.BeginTransactionAsync
            let! dbResult =
                parsedData
                |> List.fold (fun acc avail ->
                    task {
                        let! accResult = acc
                        match accResult with
                        | Error dberror -> return Error dberror
                        | Ok ids ->
                            let! insResult = queries.InsertDefaultAvailability avail
                            return insResult |> Result.map (fun id -> ids @ [ id ])
                    }
                ) (Task.FromResult(Ok []))
            queries.CommitTransaction
            return dbResult |> Result.mapError getDbErrorsResponse
    }

let crudDefaultAvailability (queries: ICalendarQueries) : HttpHandler =
    Auth.processWithTeacherId
        (fun teacherId ctx -> 
            task {
                let! json = Request.getJson<CreateEditDefaultAvailability> ctx
                let! submissionResult = crudDefaultAvailabilityFromForm json teacherId queries
                return! getFormCreateResponse submissionResult ctx
            }
        )

let getDefaultAvailability (queries: ICalendarQueries) : HttpHandler =
    Auth.processWithTeacherId
        (fun teacherId ctx ->
            task {
                let! availabilityRecords = queries.GetDefaultAvailabilityAsync teacherId
                let transformedRecords: ViewDefaultAvailability =
                    {
                        Availabilities =
                            availabilityRecords
                            |> Seq.map (fun dbRec ->
                                {
                                    DefaultAvailabilityId = dbRec.DefaultAvailabilityId
                                    DayOfWeek = dbRec.DayOfWeek.ToString()
                                    StartTime = System.DateTimeOffset.FromUnixTimeSeconds(dbRec.StartTimeUnix).ToString("hh:mm")
                                    EndTime = System.DateTimeOffset.FromUnixTimeSeconds(dbRec.EndTimeUnix).ToString("hh:mm")
                                }
                            )
                    }
                return! getHttpRecordResponse (Ok transformedRecords) ctx
            }
        )

let createRecurringEvent (queries: ICalendarQueries) : HttpHandler =
    Auth.processWithTeacherId
        (fun teacherId ctx -> 
            task {
                let submissionResult = Ok getGenericSuccess
                let httpFormResponse = getFormCreateResponse submissionResult
                return! httpFormResponse ctx
            }
        )

let editRecurringEvent (queries: ICalendarQueries) : HttpHandler =
    Auth.processWithTeacherId
        (fun teacherId ctx -> 
            task {
                let submissionResult = Ok getGenericSuccess
                let httpFormResponse = getFormEditResponse submissionResult
                return! httpFormResponse ctx
            }
        )

let createSingleEvent (queries: ICalendarQueries) : HttpHandler =
    Auth.processWithTeacherId
        (fun teacherId ctx -> 
            task {
                let submissionResult = Ok getGenericSuccess
                let httpFormResponse = getFormCreateResponse submissionResult
                return! httpFormResponse ctx
            }
        )

let editSingleEvent (queries: ICalendarQueries) : HttpHandler =
    Auth.processWithTeacherId
        (fun teacherId ctx -> 
            task {
                let submissionResult = Ok getGenericSuccess
                let httpFormResponse = getFormEditResponse submissionResult
                return! httpFormResponse ctx
            }
        )
