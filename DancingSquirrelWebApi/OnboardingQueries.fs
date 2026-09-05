module Onboarding.Queries

open DbLayer.Database
open DbLayer.Database.main
open GenericModels
open Global
open SqlHydra.Query
open System.Threading.Tasks
open Onboarding.Models

type ITrainingRequestQueries =
    abstract member InsertTrainingRequest: TrainingRequestForm -> Task<Result<GenericModelResponse<bool>, GenericModelResponse<TrainingRequestValidation>>>
    abstract member InsertOnboardedClient: string -> OnboardingRequest -> TrainingRequest -> Task<Result<TrainingRequest, GenericModelResponse<string>>>
    abstract member SelectSingleTrainingRequest: int64 -> Task<Result<TrainingRequest, RecordRetrievalErrors>>
    abstract member SelectMultiTrainingRequests: int -> int -> Task<Result<seq<TrainingRequest>, RecordRetrievalErrors>>
    abstract member CountTrainingRequests: Task<Result<int, RecordRetrievalErrors>>

type TrainingRequestQueries(db: QueryContextFactory) =
    interface ITrainingRequestQueries with
        member _.InsertTrainingRequest(form: TrainingRequestForm) =
            task {
                use! context = db.OpenContextAsync()
                try
                    insertTask context {
                        for s in TrainingRequest do
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
                    return Ok getGenericSuccess
                with
                | ex ->
                    printfn "SQL: %O" ex
                    return Error internalErrorResponse
            }

        member _.InsertOnboardedClient (onboardingUsername: string) (onboardingRequest: OnboardingRequest) (trainingRequest: TrainingRequest) =
            task {
                use! shared = db.OpenContextAsync()
                try
                    shared.BeginTransaction()
                    let caretakerType = enum<CaretakerType>(int32 trainingRequest.CaretakerType)
                    let! personOrOrganizationId =
                        match caretakerType with
                        | CaretakerType.Person ->
                            insertTask shared {
                                for p in Person do
                                entity {
                                    PersonId = 0;
                                    FirstName = trainingRequest.OwnerFirstName |?? lazy "";
                                    LastName = trainingRequest.OwnerLastName |?? lazy "";
                                }
                                getId p.PersonId
                            }
                        | _ ->
                            insertTask shared {
                                for o in Organization do
                                entity {
                                    OrganizationId = 0;
                                    Name = trainingRequest.OrganizationName |?? lazy "";
                                }
                                getId o.OrganizationId
                            }
                    let! ownerId =
                        insertTask shared {
                            for so in SquirrelOwner do
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
                        for s in Squirrel do
                        entity {
                            SquirrelId = 0;
                            Name = trainingRequest.SquirrelName;
                            SquirrelOwnerId = ownerId;
                        }
                        getId s.SquirrelId
                    }
                    let squirrelTeacherEntitites =
                        onboardingRequest.DanceTeachers
                        |> Array.map (fun teacherId ->
                            let newEntity:SquirrelTeacher = {
                                SquirrelId = squirrelId;
                                TeacherId = teacherId;
                            }
                            newEntity)
                    insertTask shared {
                        into SquirrelTeacher
                        entities squirrelTeacherEntitites
                    } |> ignore
                    let nowUnix = System.DateTimeOffset(System.DateTime.UtcNow).ToUnixTimeSeconds()
                    let! updateSuccess = updateTask shared {
                        for tr in TrainingRequest do
                        set tr.SquirrelId (Some squirrelId)
                        set tr.OnboardUsername (Some onboardingUsername)
                        set tr.OnboardingDateTimeUnix (Some nowUnix)
                        where (tr.TrainingRequestId = trainingRequest.TrainingRequestId)
                    }
                    match updateSuccess with
                        | 1 ->
                            shared.CommitTransaction()
                            let updatedRecord : TrainingRequest = {
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

        member _.SelectSingleTrainingRequest(recordId: int64) =
            task {
                try
                    let! request =
                        selectTask db {
                            for s in TrainingRequest do
                            where (s.TrainingRequestId = recordId)
                            take 2
                        }
                    let recordCount = request |> Seq.length
                    let response =
                        match recordCount with
                        | 0 -> Error RecordRetrievalErrors.NotFound
                        | 1 -> Ok (request |> Seq.head)
                        | _ -> Error RecordRetrievalErrors.ExpectedSingleFoundMultiple
                    return response
                with
                | ex ->
                    printfn "SQL: %O" ex
                    return Error RecordRetrievalErrors.DbAccessError
            }

        member _.SelectMultiTrainingRequests (skipNumber: int) (length: int) =
            task {
                try
                    let! requests =
                        selectTask db {
                            for s in TrainingRequest do
                            where (s.SquirrelId = None)
                            skip skipNumber
                            take length
                        }
                    return Ok requests
                with
                | ex ->
                    printfn "SQL: %O" ex
                    return Error RecordRetrievalErrors.DbAccessError
            }

        member _.CountTrainingRequests =
            task {
                try
                    let! requests =
                        selectTask db {
                            for s in TrainingRequest do
                            where (s.SquirrelId = None)
                            count
                        }
                    return Ok requests
                with
                | ex ->
                    printfn "SQL: %O" ex
                    return Error RecordRetrievalErrors.DbAccessError
            }
