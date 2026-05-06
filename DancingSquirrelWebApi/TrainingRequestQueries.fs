module TrainingRequest.Queries

open DbLayer
open ExternalDependencies
open GenericModels
open TrainingRequest.Models
open SqlHydra.Query

let insertRequestToDatabase (form : TrainingRequestForm) (env : IGetDb) =
    task {
        let db = env.GetDb()
        use! shared = db.OpenContextAsync()
        try
            insertTask shared {
                for s in Database.main.TrainingRequest do
                entity {
                    TrainingRequestId = 1;
                    SquirrelName = form.SquirrelName;
                    CaretakerType = int64 form.CaretakerType;
                    OrganizationName = form.CaretakerCompanyName;
                    OwnerFirstName = form.CaretakerFirstName;
                    OwnerLastName = form.CaretakerLastName;
                    Email = form.Email;
                    Phone = Some form.Phone;
                    DescriptionOfNeeds = Some form.DescriptionOfNeeds;
                    SquirrelId = None;
                    OnboardUsername = None;
                    OnboardingDateTime = None;
                }
                getId s.TrainingRequestId
            } |> ignore
            return Ok {
                IsSuccess = true
                IsInternalError = false
                ValidationFailures = None
            }            
        with
        | ex ->
            printfn "SQL: %O" ex
            return Error internalErrorResponse
    }            

let onboardClientInDb (env : IGetDb) (onboardingUsername: string) (trainingRequest : DbLayer.Database.main.TrainingRequest) =
    task {
        let db = env.GetDb()
        use! shared = db.OpenContextAsync()
        try
            shared.BeginTransaction()
            let caretakerType = enum<CaretakerType>(int32 trainingRequest.CaretakerType)
            let! personOrOrganizationId =
                match caretakerType with
                | CaretakerType.Person ->
                    insertTask shared {
                        for p in Database.main.Person do
                        entity {
                            PersonId = 0;
                            FirstName = match trainingRequest.OwnerFirstName with | None -> "" | Some s -> s;
                            LastName = match trainingRequest.OwnerLastName with | None -> "" | Some s -> s;
                        }
                        getId p.PersonId
                    }
                | _ ->
                    insertTask shared {
                        for o in Database.main.Organization do
                        entity {
                            OrganizationId = 0;
                            Name = match trainingRequest.OrganizationName with | None -> "" | Some s -> s;
                        }
                        getId o.OrganizationId
                    }
            let! ownerId =
                insertTask shared {
                    for so in Database.main.SquirrelOwner do
                    entity {
                        SquirrelOwnerId = 0;
                        PersonId = if caretakerType = CaretakerType.Person then Some personOrOrganizationId else None;
                        OrganizationId = if caretakerType = CaretakerType.Company then Some personOrOrganizationId else None;
                        PhoneNumber = trainingRequest.Phone;
                        Email = Some trainingRequest.Email;
                    }
                    getId so.SquirrelOwnerId
                }
            let! squirrelId = insertTask shared {
                for s in Database.main.Squirrel do
                entity {
                    SquirrelId = 0;
                    Name = trainingRequest.SquirrelName;
                    SquirrelOwnerId = ownerId;
                }
                getId s.SquirrelId
            }
            let dateString = System.DateTime.UtcNow.ToString "yyyy-MM-dd hh:mm:ss"
            let! updateSuccess = updateTask shared {
                for tr in Database.main.TrainingRequest do
                set tr.SquirrelId (Some squirrelId)
                set tr.OnboardUsername (Some onboardingUsername)
                set tr.OnboardingDateTime (Some dateString)
                where (tr.TrainingRequestId = trainingRequest.TrainingRequestId)
            }
            match updateSuccess with
                | 1 ->
                    shared.CommitTransaction()
                    let updatedRecord : DbLayer.Database.main.TrainingRequest = {
                        TrainingRequestId = trainingRequest.TrainingRequestId;
                        SquirrelName = trainingRequest.SquirrelName;
                        CaretakerType = trainingRequest.CaretakerType;
                        OrganizationName = trainingRequest.OrganizationName;
                        OwnerLastName = trainingRequest.OwnerLastName;
                        OwnerFirstName = trainingRequest.OwnerFirstName;
                        Email = trainingRequest.Email;
                        Phone = trainingRequest.Phone;
                        SquirrelId = (Some squirrelId);
                        OnboardUsername = (Some onboardingUsername);
                        OnboardingDateTime = (Some dateString);
                        DescriptionOfNeeds = trainingRequest.DescriptionOfNeeds;
                    }
                    return Ok updatedRecord
                | _ ->
                    shared.RollbackTransaction()
                    printfn "Update statement failed when onboarding the client"
                    return Error internalErrorResponse
        with
        | ex ->
            shared.RollbackTransaction()
            printfn "SQL: %O" ex
            return Error internalErrorResponse
    }

let getSingleTrainingRequestFromDb (env : IGetDb) (recordId : int64) =
    task {
        let db = env.GetDb()
        try
            let! request =
                selectTask db {
                    for s in Database.main.TrainingRequest do
                    where (s.TrainingRequestId = recordId)
                    take 2
                }
            let recordCount = request |> Seq.length
            let response = 
                match recordCount with
                | 0 -> Error notFoundResponse
                | 1 -> Ok (request |> Seq.head)
                | _ -> Error foundMultipleRecordsResponse
            return response
        with
        | ex ->
            printfn "SQL: %O" ex
            return Error internalErrorResponse
    }

let getTrainingRequestsFromDb (env : IGetDb) (skipNumber : int) (length : int) =
    task {
        let db = env.GetDb()
        try
            let! requests =
                selectTask db {
                    for s in Database.main.TrainingRequest do
                    where (s.SquirrelId = None)
                    skip skipNumber
                    take length
                }
            return Ok requests
        with
        | ex ->
            printfn "SQL: %O" ex
            return Error internalErrorResponse
    }

let getTrainingRequestCount (env : IGetDb) =
    task {
        let db = env.GetDb()
        try
            let! requests =
                selectTask db {
                    for s in Database.main.TrainingRequest do
                    where (s.SquirrelId = None)
                    count
                }
            return Ok requests
        with
        | ex ->
            printfn "SQL: %O" ex
            return Error internalErrorResponse
    }