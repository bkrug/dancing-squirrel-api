module Calendar.Models

open System.Threading.Tasks
open GenericModels
open DbLayer.Database

type CreateEditDefaultDayAvailability =
    { 
        TeacherId: int64
        DayOfWeek: Option<string>
        StartTime: Option<string>
        EndTime: Option<string>
    }

type CreateEditDefaultAvailability =
    {
        Availabilities: CreateEditDefaultDayAvailability[]
    }

type DefaultDayAvailabilityValidation =
    { 
        TeacherId: string
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

type DefaultActivityUpserter<'a> = CreateEditDefaultAvailability -> Task<Result<GenericModelResponse<'a>, GenericModelResponse<DefaultAvailabilityValidation>>>
type RecurringEventUpserter<'a> = CreateEditRecurringEvent -> Task<Result<GenericModelResponse<'a>, GenericModelResponse<CreateEditRecurringEvent>>>
type SingleEventUpserter<'a> = CreateEditSingleEvent -> Task<Result<GenericModelResponse<'a>, GenericModelResponse<CreateEditSingleEvent>>>