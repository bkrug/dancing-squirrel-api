module DanceType.Queries

open DbLayer
open GenericModels
open SqlHydra.Query

type DanceTypeQueries(db: Database.QueryContextFactory) =
    member _.SelectDanceTypes =
        task {
            try
                let! danceTypes =
                    selectTask db {
                        for dt in Database.main.DanceType do
                        select dt
                    }
                return Ok danceTypes
            with
            | ex ->
                printfn "SQL: %O" ex
                return Error internalErrorResponse
        }

    member _.SelectTeachersByDanceType(danceTypeId: int64) =
        task {
            try
                let! teachers =
                    selectTask db {
                        for dtt in Database.main.DanceTypeTeacher do
                        join t in Database.main.Teacher on (dtt.TeacherId = t.TeacherId)
                        where (dtt.DanceTypeId = danceTypeId)
                        select t
                    }
                return Ok teachers
            with
            | ex ->
                printfn "SQL: %O" ex
                return Error internalErrorResponse
        }
