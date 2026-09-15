module Calendar.Queries

open System.Threading.Tasks
open DbLayer
open DbLayer.Database.main
open GenericModels
open SqlHydra.Query

type ICalendarQueries =
    abstract member BeginTransactionAsync : Task<unit>
    abstract member CommitTransaction : unit
    abstract member GetDefaultAvailabilityAsync : int -> Task<seq<DefaultAvailability>>
    abstract member InsertDefaultAvailability : DefaultAvailability -> Task<Result<int64, DbErrors>>
    abstract member UpdateDefaultAvailability : DefaultAvailability -> Task<Result<unit, DbErrors>>
    abstract member DeleteDefaultAvailability : int -> Task<Result<unit, DbErrors>>
    abstract member InsertRecurringEvent : RecurringEvent -> Task<Result<RecurringEvent, DbErrors>>
    abstract member UpdateRecurringEvent : RecurringEvent -> Task<Result<RecurringEvent, DbErrors>>

type CalendarQueries(db: Database.QueryContextFactory) =
    let mutable context : QueryContext = Unchecked.defaultof<QueryContext>

    interface ICalendarQueries with
        member _.BeginTransactionAsync =
            task {
                let! context = db.OpenContextAsync()
                context.BeginTransaction()
            }

        member _.CommitTransaction =
            context.CommitTransaction()
            context.Dispose()

        member _.GetDefaultAvailabilityAsync (teacherId: int): Task<seq<DefaultAvailability>> = 
            task {
                let! defaultAvailabilities =
                    selectTask db {
                        for a in DefaultAvailability do
                        where (a.TeacherId = teacherId)
                    }
                return defaultAvailabilities
            }

        member _.InsertDefaultAvailability (availability: DefaultAvailability) : Task<Result<int64, DbErrors>> =
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

        member _.UpdateDefaultAvailability (availability: DefaultAvailability) : Task<Result<unit, DbErrors>> =
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

        member _.DeleteDefaultAvailability (defaultAvailabilityId : int) =
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

        member _.InsertRecurringEvent (recurringEvent: RecurringEvent) : Task<Result<RecurringEvent, DbErrors>> =
            failwith "Not implemented"

        member _.UpdateRecurringEvent (recurringEvent: RecurringEvent) : Task<Result<RecurringEvent, DbErrors>> =
            failwith "Not implemented"
