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
        ModelFailure: string
        DayOfWeek: string
        StartTime: string
        EndTime: string
    }    

type DefaultAvailabilityValidation =
    {
        ModelFailure: string
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