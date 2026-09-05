module Calendar.Queries

open System.Threading.Tasks
open Calendar.Models
open DbLayer
open GenericModels

type ICalendarQueries =
    abstract member UpsertDefaultAvailability : CreateEditDefaultAvailability -> Task<Result<GenericModelResponse<bool>, GenericModelResponse<DefaultAvailabilityValidation>>>
    abstract member UpsertRecurringEvent : CreateEditRecurringEvent -> Task<Result<GenericModelResponse<bool>, GenericModelResponse<CreateEditRecurringEvent>>>
    abstract member UpsertSingleEvent : CreateEditSingleEvent -> Task<Result<GenericModelResponse<bool>, GenericModelResponse<CreateEditSingleEvent>>>

type CalendarQueries(db: Database.QueryContextFactory) =
    interface ICalendarQueries with
        member _.UpsertDefaultAvailability (availability: CreateEditDefaultAvailability) : Task<Result<GenericModelResponse<bool>, GenericModelResponse<DefaultAvailabilityValidation>>> =
            failwith "Not implemented"

        member _.UpsertRecurringEvent (recurringEvent: CreateEditRecurringEvent) : Task<Result<GenericModelResponse<bool>, GenericModelResponse<CreateEditRecurringEvent>>> =
            failwith "Not implemented"

        member _.UpsertSingleEvent (singleEvent: CreateEditSingleEvent) : Task<Result<GenericModelResponse<bool>, GenericModelResponse<CreateEditSingleEvent>>> =
            failwith "Not implemented"
