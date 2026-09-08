module Calendar.Queries

open System.Threading.Tasks
open DbLayer
open DbLayer.Database.main
open GenericModels

type ICalendarQueries =
    abstract member UpsertDefaultAvailability : DefaultAvailability -> Task<Result<DefaultAvailability, RecordInsertError>>
    abstract member UpsertRecurringEvent : RecurringEvent -> Task<Result<RecurringEvent, RecordInsertError>>

type CalendarQueries(db: Database.QueryContextFactory) =
    interface ICalendarQueries with
        member _.UpsertDefaultAvailability (availability: DefaultAvailability) : Task<Result<DefaultAvailability, RecordInsertError>> =
            failwith "Not implemented"

        member _.UpsertRecurringEvent (recurringEvent: RecurringEvent) : Task<Result<RecurringEvent, RecordInsertError>> =
            failwith "Not implemented"
