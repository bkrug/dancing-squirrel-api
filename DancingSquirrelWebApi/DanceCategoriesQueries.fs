module DanceCategories.Queries

open DbLayer.Database
open DbLayer.Database.main
open GenericModels
open SqlHydra.Query
open System.Threading.Tasks

type IDanceTypeQueries =
    abstract member SelectDanceTypes: Task<Result<seq<DanceType>, DbErrors>>
    abstract member SelectTeachersByDanceType: int64 -> Task<Result<seq<Teacher>, DbErrors>>

type DanceTypeQueries(db: QueryContextFactory) =
    interface IDanceTypeQueries with
        member _.SelectDanceTypes =
            task {
                try
                    let! danceTypes =
                        selectTask db {
                            for dt in DanceType do
                            select dt
                        }
                    return Ok danceTypes
                with
                | ex ->
                    printfn "SQL: %O" ex
                    return Error DbErrors.AccessError
            }

        member _.SelectTeachersByDanceType(danceTypeId: int64) =
            task {
                try
                    let! teachers =
                        selectTask db {
                            for dtt in DanceTypeTeacher do
                            join t in Teacher on (dtt.TeacherId = t.TeacherId)
                            where (dtt.DanceTypeId = danceTypeId)
                            select t
                        }
                    return Ok teachers
                with
                | ex ->
                    printfn "SQL: %O" ex
                    return Error DbErrors.AccessError
            }
