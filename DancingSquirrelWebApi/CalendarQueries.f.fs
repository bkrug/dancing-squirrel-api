module Calendar.Queries

open System.Threading.Tasks
open DbLayer
open DbLayer.Database.main
open GenericModels

type ICalendarQueries =
    abstract member InsertDefaultAvailability : list<DefaultAvailability> -> Task<Result<list<DefaultAvailability>, DbErrors>>
    abstract member UpdateDefaultAvailability : list<DefaultAvailability> -> Task<Result<list<DefaultAvailability>, DbErrors>>
    abstract member InsertRecurringEvent : RecurringEvent -> Task<Result<RecurringEvent, DbErrors>>
    abstract member UpdateRecurringEvent : RecurringEvent -> Task<Result<RecurringEvent, DbErrors>>

type CalendarQueries(db: Database.QueryContextFactory) =
    interface ICalendarQueries with
        member _.InsertDefaultAvailability (availabilities: list<DefaultAvailability>) : Task<Result<list<DefaultAvailability>, DbErrors>> =
            failwith "Not implemented"            

        member _.UpdateDefaultAvailability (availabilities: list<DefaultAvailability>) : Task<Result<list<DefaultAvailability>, DbErrors>> =
            failwith "Not implemented"

        member _.InsertRecurringEvent (recurringEvent: RecurringEvent) : Task<Result<RecurringEvent, DbErrors>> =
            failwith "Not implemented"

        member _.UpdateRecurringEvent (recurringEvent: RecurringEvent) : Task<Result<RecurringEvent, DbErrors>> =
            failwith "Not implemented"
