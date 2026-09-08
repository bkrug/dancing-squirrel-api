module DanceCategories.Endpoints

open Falco
open GenericModels
open DanceCategories.Queries

let private roles = [OnboarderRole]

let getDanceTypes (queries: IDanceTypeQueries) =
    Auth.processAuthorizedRequest roles
        (fun ctx ->
            task {
                let! danceTypes =
                    queries.SelectDanceTypes
                    |> TaskResult.mapError getRecordRetrievalErrorResponse
                return! getHttpRecordResponse danceTypes ctx
            }
        )

let getTeachersByDanceType (queries: IDanceTypeQueries) =
    Auth.processAuthorizedRequest roles
        (fun ctx ->
            task {
                let danceTypeId = (Request.getRoute ctx).GetInt64("danceTypeId")
                let! teachers =
                    queries.SelectTeachersByDanceType danceTypeId
                    |> TaskResult.mapError getRecordRetrievalErrorResponse
                return! getHttpRecordResponse teachers ctx
            }
        )
