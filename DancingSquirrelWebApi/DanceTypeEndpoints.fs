module DanceType.Endpoints

open Falco
open GenericModels
open DanceType.Models

let getDanceTypes (selectDanceTypes: DanceTypeSelector<'a>) =
    Auth.processAuthenticatedRequest
        (fun ctx ->
            task {
                let! danceTypes = selectDanceTypes
                let httpResponse = getHttpRecordResponse danceTypes
                return! httpResponse ctx
            }
        )

let getTeachersByDanceType (selectTeachers: TeachersByDanceTypeSelector<'a>) =
    Auth.processAuthenticatedRequest
        (fun ctx ->
            task {
                let danceTypeId = (Request.getRoute ctx).GetInt64("danceTypeId")
                let! teachers = selectTeachers danceTypeId
                let httpResponse = getHttpRecordResponse teachers
                return! httpResponse ctx
            }
        )
