module Calendar.Queries

open System.Threading.Tasks
open DbLayer
open DbLayer.Database.main
open GenericModels

type ICalendarQueries =
    abstract member UpsertDefaultAvailability : list<DefaultAvailability> -> Task<Result<list<DefaultAvailability>, RecordInsertError>>
    abstract member UpsertRecurringEvent : RecurringEvent -> Task<Result<RecurringEvent, RecordInsertError>>

type CalendarQueries(db: Database.QueryContextFactory) =
    interface ICalendarQueries with
        member _.UpsertDefaultAvailability (availabilities: list<DefaultAvailability>) : Task<Result<list<DefaultAvailability>, RecordInsertError>> =
            failwith "Not implemented"

        member _.UpsertRecurringEvent (recurringEvent: RecurringEvent) : Task<Result<RecurringEvent, RecordInsertError>> =
            failwith "Not implemented"
