module Calendar.Queries

open System.Threading.Tasks
open DbLayer
open DbLayer.Database.main
open GenericModels
open SqlHydra.Query

type ICalendarQueries =
    abstract member InsertDefaultAvailability : list<DefaultAvailability> -> Task<Result<list<DefaultAvailability>, DbErrors>>
    abstract member UpdateDefaultAvailability : list<DefaultAvailability> -> Task<Result<list<DefaultAvailability>, DbErrors>>
    abstract member InsertRecurringEvent : RecurringEvent -> Task<Result<RecurringEvent, DbErrors>>
    abstract member UpdateRecurringEvent : RecurringEvent -> Task<Result<RecurringEvent, DbErrors>>

type CalendarQueries(db: Database.QueryContextFactory) =
    interface ICalendarQueries with
        member _.InsertDefaultAvailability (availabilities: list<DefaultAvailability>) : Task<Result<list<DefaultAvailability>, DbErrors>> =
            task {
                use! shared = db.OpenContextAsync()
                try
                    shared.BeginTransaction()
                    let insertedAvailabilities = ResizeArray<DefaultAvailability>()
                    for availability in availabilities do
                        let! newId =
                            insertTask shared {
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
                        insertedAvailabilities.Add { availability with DefaultAvailabilityId = newId }
                    shared.CommitTransaction()
                    return Ok (insertedAvailabilities |> List.ofSeq)
                with
                | ex ->
                    shared.RollbackTransaction()
                    printfn "SQL: %O" ex
                    return Error DbErrors.AccessError
            }

        member _.UpdateDefaultAvailability (availabilities: list<DefaultAvailability>) : Task<Result<list<DefaultAvailability>, DbErrors>> =
            task {
                use! shared = db.OpenContextAsync()
                try
                    shared.BeginTransaction()
                    let updatedAvailabilities = ResizeArray<DefaultAvailability>()
                    for availability in availabilities do
                        let! rowsUpdated =
                            updateTask shared {
                                for da in DefaultAvailability do
                                set da.TeacherId availability.TeacherId
                                set da.DayOfWeek availability.DayOfWeek
                                set da.StartTimeUnix availability.StartTimeUnix
                                set da.EndTimeUnix availability.EndTimeUnix
                                where (da.DefaultAvailabilityId = availability.DefaultAvailabilityId)
                            }
                        match rowsUpdated with
                        | 1 -> updatedAvailabilities.Add availability
                        | _ -> failwith $"Update affected {rowsUpdated} rows for DefaultAvailabilityId {availability.DefaultAvailabilityId}"
                    shared.CommitTransaction()
                    return Ok (updatedAvailabilities |> List.ofSeq)
                with
                | ex ->
                    shared.RollbackTransaction()
                    printfn "SQL: %O" ex
                    return Error DbErrors.AccessError
            }

        member _.InsertRecurringEvent (recurringEvent: RecurringEvent) : Task<Result<RecurringEvent, DbErrors>> =
            failwith "Not implemented"

        member _.UpdateRecurringEvent (recurringEvent: RecurringEvent) : Task<Result<RecurringEvent, DbErrors>> =
            failwith "Not implemented"
