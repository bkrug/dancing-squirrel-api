module TrainingRequest.Models

type CaretakerType =
    | Person = 1
    | Company = 2

type TrainingRequestValidation =
    {
        CaretakerType: string;
        CaretakerFirstName: string;
        CaretakerLastName: string;
        CaretakerCompanyName: string
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