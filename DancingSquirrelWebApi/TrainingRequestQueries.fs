module TrainingRequest.Queries

open DbLayer
open GenericModels
open SqlHydra.Query
open TrainingRequest.Models

let trainingRequestInsertionFactory (db:Database.QueryContextFactory) : TrainingRequestFormInserter<'a> =
    let insertTrainingRequest form =
        task {
            use! context = db.OpenContextAsync()
            try
                insertTask context {
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
                        OnboardingDateTimeUnix = None;
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
    insertTrainingRequest

let OnboardedClientInsertionFactory (db:Database.QueryContextFactory) : OnboardedClientInserter<'a> = 
    let insertOnboardedClient (onboardingUsername: string) (trainingRequest : DbLayer.Database.main.TrainingRequest) =
        task {
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
                let nowUnix = System.DateTimeOffset(System.DateTime.UtcNow).ToUnixTimeSeconds()
                let! updateSuccess = updateTask shared {
                    for tr in Database.main.TrainingRequest do
                    set tr.SquirrelId (Some squirrelId)
                    set tr.OnboardUsername (Some onboardingUsername)
                    set tr.OnboardingDateTimeUnix (Some nowUnix)
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
                            OnboardingDateTimeUnix = (Some nowUnix);
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
    insertOnboardedClient

let singleTrainingRequestSelectionFactory (db:Database.QueryContextFactory) : SingleTrainingRequestSelector =
    let selectSingleTrainingRequest (recordId : int64) =
        task {
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
    selectSingleTrainingRequest

let multiTrainingRequestSelectionFactory (db:Database.QueryContextFactory) : MultiTrainingRequestSelector<'a> =
    let selectMultiTrainingRequests skipNumber length =
        task {
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
    selectMultiTrainingRequests

let trainingRequestCounterFactory (db:Database.QueryContextFactory) : TrainingRequestCounter<'a> =
    let countTrainingRequests =
        task {
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
    countTrainingRequests