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
    (upsertRecord: DefaultAvailability[] -> Task<Result<DefaultAvailability, RecordInsertError>>)
    : Task<Result<bool, GenericModelResponse<DefaultAvailabilityValidation>>> =
    task {
        return Ok true
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
