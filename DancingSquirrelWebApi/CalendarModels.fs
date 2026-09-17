module Calendar.Models

open System.Threading.Tasks
open GenericModels

type ViewDefaultDayAvailability =
    {
        DefaultAvailabilityId: int64
        DayOfWeek: string
        StartTime: string
        EndTime: string
    }

type ViewDefaultAvailability =
    {
        Availabilities: list<ViewDefaultDayAvailability>
    }

type CreateEditDefaultDayAvailability =
    { 
        DefaultAvailabilityId: Option<int64>
        DayOfWeek: Option<string>
        StartTime: Option<string>
        EndTime: Option<string>
    }

type CreateEditDefaultAvailability =
    {
        Availabilities: option<list<CreateEditDefaultDayAvailability>>
    }

type DefaultDayAvailabilityValidation =
    { 
        DayOfWeek: string
        StartTime: string
        EndTime: string
    }    

type DefaultAvailabilityValidation =
    {
        Availabilities: DefaultDayAvailabilityValidation[]
    }    

type CreateEditRecurringEvent =
    {
        Description: string
        DaysOfWeek: string[]
        StartDate: string
        EndDate: string
        StartTime: string
        EndTime: string
    }

type CreateEditSingleEvent =
    {
        Description: string
        StartDateTime: string
        EndDateTeim: string
    }

type ValidationDictionary =
    {
        FieldFailures: Map<string, string>
        GridFailures: Map<string, list<ValidationDictionary>>
        ModelFailure: Option<string>
    }