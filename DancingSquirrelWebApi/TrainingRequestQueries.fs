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

let insertRequestToDatabaseOld (form : TrainingRequestForm) (env : IGetDb) =
    task {
        let db = env.GetDb()
        use! shared = db.OpenContextAsync()
        shared.BeginTransaction()
        try
            let! personOrOrganizationId =
                match form.CaretakerType with
                | CaretakerType.Person ->
                    insertTask shared {
                        for p in Database.main.Person do
                        entity { PersonId = 1; FirstName = form.CaretakerFirstName.Value; LastName = form.CaretakerLastName.Value }
                        getId p.PersonId
                    }
                | _ ->
                    insertTask shared {
                        for o in Database.main.Organization do
                        entity { OrganizationId = 1; Name = form.CaretakerCompanyName.Value }
                        getId o.OrganizationId
                    }
            let! ownerId =
                insertTask shared {
                    for so in Database.main.SquirrelOwner do
                    entity {
                        SquirrelOwnerId = 0;
                        PersonId = if form.CaretakerType = CaretakerType.Person then Some personOrOrganizationId else None;
                        OrganizationId = if form.CaretakerType = CaretakerType.Company then None else Some personOrOrganizationId;
                        PhoneNumber = Some form.Phone;
                        Email = Some form.Email;
                    }
                    getId so.SquirrelOwnerId
                }
            insertTask shared {
                for s in Database.main.Squirrel do
                entity { SquirrelId = 0; Name = form.SquirrelName; SquirrelOwnerId = ownerId }
                getId s.SquirrelId
            } |> ignore
            shared.CommitTransaction()
            return Ok {
                IsSuccess = true
                IsInternalError = false
                ValidationFailures = None
            }            
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