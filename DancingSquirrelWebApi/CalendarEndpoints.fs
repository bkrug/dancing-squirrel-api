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
let roles = ["TeacherRole"]

let createDefaultAvailabilityFromForm
    (form: CreateEditDefaultAvailability)
    (upsertRecords: list<DefaultAvailability> -> Task<Result<list<DefaultAvailability>, RecordInsertError>>) =
    //: Task<Result<bool, GenericModelResponse<DefaultAvailabilityValidation>>> =
    task {
        let parsedData =
            form.Availabilities
            |> Seq.map (fun a ->
                let dayOfWeek = 
                    match a.DayOfWeek with
                    | Some dayOfWeekString -> 
                        match Enum.TryParse<DayOfWeek> dayOfWeekString with
                        | true, dayValue -> int64 dayValue
                        | _ -> int64 -1
                    | None -> int64 -1
                let startTime = 
                    match a.StartTime with
                    | Some timeString ->
                        match TimeOnly.TryParse timeString with
                        | true, parsedTime ->
                            int64 (parsedTime.Hour * 60 * 60 + parsedTime.Minute * 60 * 60)
                        | _ ->
                            int64 -1
                    | None -> int64 -1
                let endTime = 
                    match a.EndTime with
                    | Some timeString ->
                        match TimeOnly.TryParse timeString with
                        | true, parsedTime ->
                            int64 (parsedTime.Hour * 60 * 60 + parsedTime.Minute * 60 * 60)
                        | _ ->
                            int64 -1
                    | None -> int64 -1                
                let dbA : DefaultAvailability =
                    {
                        TeacherId = a.TeacherId;
                        DayOfWeek = dayOfWeek;
                        StartTimeUnix = startTime;
                        EndTimeUnix = endTime;
                        DefaultAvailabilityId = 0;
                    }
                dbA
            )
            |> Seq.toList
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
