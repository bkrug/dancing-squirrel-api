module Calendar.Endpoints

open DbLayer.Database
open DbLayer.Database.main
open Falco
open GenericModels
open System
open System.Collections.Generic
open System.Text.Json
open System.Text.RegularExpressions
open System.Threading.Tasks
open Calendar.Models
open Calendar.Queries
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
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

let parseToDefaultAvailability (form: CreateEditDefaultAvailability) : Result<list<DefaultAvailability>, DefaultAvailabilityValidation> =
    let validatedRows = 
        form.Availabilities
        |> Seq.map (fun a ->
            let dayOfWeekR = getParsedDayOfWeek a.DayOfWeek
            let startTimeR = getParsedTimeOfDay a.StartTime
            let endTimeR = getParsedTimeOfDay a.EndTime
            let dbA =
                match dayOfWeekR, startTimeR, endTimeR with
                | Ok dayOfWeek, Ok startTime, Ok endTime ->
                    Ok {
                        TeacherId = a.TeacherId;
                        DayOfWeek = dayOfWeek;
                        StartTimeUnix = startTime;
                        EndTimeUnix = endTime;
                        DefaultAvailabilityId = 0;
                    }
                | _ ->
                    let validations: DefaultDayAvailabilityValidation =
                        {
                            TeacherId = "";
                            DayOfWeek = match dayOfWeekR with | Error msg -> msg | _ -> "";
                            StartTime = match startTimeR with | Error msg -> msg | _ -> "";
                            EndTime = match endTimeR with | Error msg -> msg | _ -> "";
                        }
                    Error validations
            dbA
        )
    let emptySeq : seq<DefaultAvailability> = []
    let successOrFail =
        (Ok emptySeq, validatedRows)
        ||> Seq.fold (fun acc rowParseResult ->
            match acc, rowParseResult with
            | Error prevErrors, Error newError ->
                Error (Seq.append prevErrors [newError])
            | Error prevErrors, Ok _ ->
                Error (Seq.append prevErrors [ { TeacherId = ""; DayOfWeek = ""; StartTime = ""; EndTime = ""; } ])
            | Ok prevSuccess, Error newError ->
                Error (
                    seq {
                        for i in 1..Seq.length prevSuccess do
                            yield { TeacherId = ""; DayOfWeek = ""; StartTime = ""; EndTime = ""; }
                        yield newError
                    }
                )
            | Ok prevSuccess, Ok newSuccess ->
                Ok (Seq.append prevSuccess [newSuccess])
        )
    match successOrFail with
        | Ok success -> Ok (Seq.toList success)
        | Error err -> Error { Availabilities = Seq.toArray err }

let createDefaultAvailabilityFromForm
    (form: CreateEditDefaultAvailability)
    (upsertRecords: list<DefaultAvailability> -> Task<Result<list<DefaultAvailability>, RecordInsertError>>) =
    task {
        match parseToDefaultAvailability form with
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
