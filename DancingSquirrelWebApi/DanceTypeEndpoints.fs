module DanceType.Endpoints

open Falco
open GenericModels
open DanceType.Queries

let getDanceTypes (queries: IDanceTypeQueries) =
    Auth.processAuthenticatedRequest
        (fun ctx ->
            task {
                let! danceTypes = queries.SelectDanceTypes
                let httpResponse = getHttpRecordResponse danceTypes
                return! httpResponse ctx
            }
        )

let getTeachersByDanceType (queries: IDanceTypeQueries) =
    Auth.processAuthenticatedRequest
        (fun ctx ->
            task {
                let danceTypeId = (Request.getRoute ctx).GetInt64("danceTypeId")
                let! teachers = queries.SelectTeachersByDanceType danceTypeId
                let httpResponse = getHttpRecordResponse teachers
                return! httpResponse ctx
            }
        )
