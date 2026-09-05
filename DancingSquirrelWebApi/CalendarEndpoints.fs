module Calendar.Endpoints

open DbLayer.Database
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

let createDefaultAvailabilityFromForm (queries: ICalendarQueries) : HttpHandler =
    Auth.processAuthorizedRequest roles
        (fun ctx ->
            task {
                let submissionResult = Ok getGenericSuccess
                let httpFormResponse = getFormCreateResponse submissionResult
                return! httpFormResponse ctx
            }
        )

let editDefaultAvailabilityFromForm (queries: ICalendarQueries) : HttpHandler =
    Auth.processAuthorizedRequest roles
        (fun ctx ->
            task {
                let submissionResult = Ok getGenericSuccess
                let httpFormResponse = getFormEditResponse submissionResult
                return! httpFormResponse ctx
            }
        )

let createRecurringEventFromForm (queries: ICalendarQueries) : HttpHandler =
    Auth.processAuthorizedRequest roles
        (fun ctx ->
            task {
                let submissionResult = Ok getGenericSuccess
                let httpFormResponse = getFormCreateResponse submissionResult
                return! httpFormResponse ctx
            }
        )

let editRecurringEventFromForm (queries: ICalendarQueries) : HttpHandler =
    Auth.processAuthorizedRequest roles
        (fun ctx ->
            task {
                let submissionResult = Ok getGenericSuccess
                let httpFormResponse = getFormEditResponse submissionResult
                return! httpFormResponse ctx
            }
        )

let createSingleEventFromForm (queries: ICalendarQueries) : HttpHandler =
    Auth.processAuthorizedRequest roles
        (fun ctx ->
            task {
                let submissionResult = Ok getGenericSuccess
                let httpFormResponse = getFormCreateResponse submissionResult
                return! httpFormResponse ctx
            }
        )

let editSingleEventFromForm (queries: ICalendarQueries) : HttpHandler =
    Auth.processAuthorizedRequest roles
        (fun ctx ->
            task {
                let submissionResult = Ok getGenericSuccess
                let httpFormResponse = getFormEditResponse submissionResult
                return! httpFormResponse ctx
            }
        )
