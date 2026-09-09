module Calendar.Queries

open System.Threading.Tasks
open DbLayer
open DbLayer.Database.main
open GenericModels

type ICalendarQueries =
    abstract member InsertDefaultAvailability : list<DefaultAvailability> -> Task<Result<list<DefaultAvailability>, RecordInsertError>>
    abstract member UpdateDefaultAvailability : list<DefaultAvailability> -> Task<Result<list<DefaultAvailability>, RecordInsertError>>
    abstract member InsertRecurringEvent : RecurringEvent -> Task<Result<RecurringEvent, RecordInsertError>>
    abstract member UpdateRecurringEvent : RecurringEvent -> Task<Result<RecurringEvent, RecordInsertError>>

type CalendarQueries(db: Database.QueryContextFactory) =
    interface ICalendarQueries with
        member _.InsertDefaultAvailability (availabilities: list<DefaultAvailability>) : Task<Result<list<DefaultAvailability>, RecordInsertError>> =
            failwith "Not implemented"            

        member _.UpdateDefaultAvailability (availabilities: list<DefaultAvailability>) : Task<Result<list<DefaultAvailability>, RecordInsertError>> =
            failwith "Not implemented"

        member _.InsertRecurringEvent (recurringEvent: RecurringEvent) : Task<Result<RecurringEvent, RecordInsertError>> =
            failwith "Not implemented"

        member _.UpdateRecurringEvent (recurringEvent: RecurringEvent) : Task<Result<RecurringEvent, RecordInsertError>> =
            failwith "Not implemented"
