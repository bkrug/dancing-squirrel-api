module Calendar.Models

type CreateEditDefaultDayAvailability =
    { 
        TeacherId: int64
        DayOfWeek: string
        StartTime: string
        EndTime: string
    }

type CreateEditDefaultAvailability =
    {
        Availabilities: CreateEditDefaultDayAvailability[]
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