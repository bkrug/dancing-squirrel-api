module Onboarding.Models

open System.Threading.Tasks
open GenericModels
open DbLayer.Database

type CaretakerType =
    | Person = 1
    | Company = 2

type TrainingRequestValidation =
    {
        CaretakerType: string;
        CaretakerFirstName: string;
        CaretakerLastName: string;
        CaretakerCompanyName: string;
        Email: string;
        Phone: string;
        SquirrelName: string;
        DescriptionOfNeeds: string;
    }

type TrainingRequestForm =
    {
        CaretakerType: CaretakerType;
        CaretakerFirstName: Option<string>;
        CaretakerLastName: Option<string>;
        CaretakerCompanyName: Option<string>;
        Email: string;
        Phone: string;
        SquirrelName: string;
        DescriptionOfNeeds: string;
    }

type OnboardingRequest =
    {
        DanceTeachers: int64[];
    }

type OnboardClientInsertError =
    | DbAccessError
    | UpdateFailed

let getOnboardClientInsertErrorResponse insertError =
    match insertError with
    | OnboardClientInsertError.DbAccessError -> internalErrorResponse
    | OnboardClientInsertError.UpdateFailed -> internalErrorResponse

type TrainingRequestFormInserter = TrainingRequestForm -> Task<Result<int64, RecordInsertError>>