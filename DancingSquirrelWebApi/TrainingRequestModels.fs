module TrainingRequest.Models

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

type TrainingRequestFormInserter<'a> = TrainingRequestForm -> Task<Result<GenericModelResponse<'a>, GenericModelResponse<TrainingRequestValidation>>>
type OnboardedClientInserter<'a> = string -> main.TrainingRequest -> Task<Result<main.TrainingRequest, GenericModelResponse<'a>>>
type SingleTrainingRequestSelector = int64 -> Task<Result<main.TrainingRequest, GenericModelResponse<string>>>
type MultiTrainingRequestSelector<'a> = int -> int -> Task<Result<seq<main.TrainingRequest>, GenericModelResponse<'a>>>
type TrainingRequestCounter<'a> = Task<Result<int, GenericModelResponse<'a>>>