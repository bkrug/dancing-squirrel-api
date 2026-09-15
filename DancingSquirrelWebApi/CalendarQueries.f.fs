module Calendar.Queries

open DbLayer
open DbLayer.Database.main
open GenericModels
open SqlHydra.Query
open System.Threading.Tasks

type ICalendarQueries =
    abstract member BeginTransactionAsync : Task<unit>
    abstract member CommitTransaction : unit
    abstract member GetDefaultAvailabilityAsync : int64 -> Task<seq<DefaultAvailability>>
    abstract member GetDefaultAvailabilityIdsAsync : int64 -> Task<seq<int64>>
    abstract member InsertDefaultAvailabilityAsync : DefaultAvailability -> Task<Result<int64, DbErrors>>
    abstract member UpdateDefaultAvailabilityAsync : DefaultAvailability -> Task<Result<unit, DbErrors>>
    abstract member DeleteDefaultAvailabilityAsync : int64 -> Task<Result<unit, DbErrors>>

type CalendarQueries(db: Database.QueryContextFactory) =
    let mutable context : QueryContext = Unchecked.defaultof<QueryContext>

    interface ICalendarQueries with
        member _.BeginTransactionAsync =
            task {
                let! c = db.OpenContextAsync()
                context <- c
                context.BeginTransaction()
            }

        member _.CommitTransaction =
            context.CommitTransaction()
            context.Dispose()

        member _.GetDefaultAvailabilityAsync (teacherId: int64): Task<seq<DefaultAvailability>> = 
            task {
                let! defaultAvailabilities =
                    selectTask db {
                        for a in DefaultAvailability do
                        where (a.TeacherId = teacherId)
                    }
                return defaultAvailabilities
            }

        member _.GetDefaultAvailabilityIdsAsync (teacherId: int64): Task<seq<int64>> = 
            task {
                let! recordIds =
                    selectTask db {
                        for a in DefaultAvailability do
                        where (a.TeacherId = teacherId)
                        select a.DefaultAvailabilityId
                    }
                return recordIds
            }            

        member _.InsertDefaultAvailabilityAsync (availability: DefaultAvailability) : Task<Result<int64, DbErrors>> =
            task {
                let! newId = insertTask context {
                    for da in DefaultAvailability do
                    entity {
                        DefaultAvailabilityId = 0;
                        TeacherId = availability.TeacherId;
                        DayOfWeek = availability.DayOfWeek;
                        StartTimeUnix = availability.StartTimeUnix;
                        EndTimeUnix = availability.EndTimeUnix;
                    }
                    getId da.DefaultAvailabilityId
                }
                return
                    match newId with
                    | 0L -> Error DbErrors.AccessError
                    | _ -> Ok newId
            }

        member _.UpdateDefaultAvailabilityAsync (availability: DefaultAvailability) : Task<Result<unit, DbErrors>> =
            task {
                let! rowsUpdated = updateTask context {
                    for da in DefaultAvailability do
                    set da.TeacherId availability.TeacherId
                    set da.DayOfWeek availability.DayOfWeek
                    set da.StartTimeUnix availability.StartTimeUnix
                    set da.EndTimeUnix availability.EndTimeUnix
                    where (da.DefaultAvailabilityId = availability.DefaultAvailabilityId)
                }
                return
                    match rowsUpdated with
                    | 0 -> Error DbErrors.NotFound
                    | 1 -> Ok()
                    | _ -> Error DbErrors.ExpectedSingleFoundMultiple
            }

        member _.DeleteDefaultAvailabilityAsync (defaultAvailabilityId : int64) =
            task {
                let! rowsDeleted = deleteTask context {
                    for da in DefaultAvailability do
                    where (da.DefaultAvailabilityId = defaultAvailabilityId)
                }
                return
                    match rowsDeleted with
                    | 0 -> Error DbErrors.NotFound
                    | 1 -> Ok()
                    | _ -> Error DbErrors.ExpectedSingleFoundMultiple
            }
