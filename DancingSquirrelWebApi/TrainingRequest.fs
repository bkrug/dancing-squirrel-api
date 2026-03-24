module TrainingRequest

open Falco
open DbLayer
open SqlHydra.Query

let connStr = "Data Source=/home/bkrug/Repos/dancing-squirrel-api/Database/DancingSquirrel.db;"
let db = Database.QueryContextFactory.Create(connStr, printfn "SQL: %O")

let getCustomer id =
    selectTask db {
        for p in Database.main.SquirrelOwner do
        where (p.SquirrelOwnerId = id)
        select p
    }

type trainingRequest =
    {
        IsPerson: bool
        CaretakerName: string
        Email: string
        Phone: string
        SquirrelName: string
    }
type trainingRequestResponse =
    {
        SquirrelId: int64
    }

let inserTrainingRequest trainingRequestModel =
    task {
        use! shared = db.OpenContextAsync()
        shared.BeginTransaction()
        try
            let! personOrOrganizationId =
                match trainingRequestModel.IsPerson with
                | true ->
                    insertTask db {
                        for p in Database.main.Person do
                        entity { PersonId = 1; FirstName = "n/a"; LastName = trainingRequestModel.CaretakerName }
                        getId p.PersonId
                    }
                | false ->
                    insertTask db {
                        for o in Database.main.Organization do
                        entity { OrganizationId = 1; Name = trainingRequestModel.CaretakerName }
                        getId o.OrganizationId
                    }
            let! ownerId =
                insertTask db {
                    for so in Database.main.SquirrelOwner do
                    entity {
                        SquirrelOwnerId = 0;
                        PersonId = if trainingRequestModel.IsPerson then Some personOrOrganizationId else None;
                        OrganizationId = if trainingRequestModel.IsPerson then None else Some personOrOrganizationId
                    }
                    getId so.SquirrelOwnerId
                }
            let! squirrelId =
                insertTask db {
                    for s in Database.main.Squirrel do
                    entity { SquirrelId = 0; Name = trainingRequestModel.SquirrelName; SquirrelOwnerId = ownerId }
                    getId s.SquirrelId
                }
            shared.CommitTransaction()
            return squirrelId
        with
        | ex ->
            return 0
    }

let createTrainingRequest : HttpHandler = fun ctx ->
    task {
        let! f = Request.getForm ctx
        let dataToSave =
            {
                IsPerson = f.GetString("caretakertype", "") = "person"
                CaretakerName = f.GetString ("caretakername", "")
                Email = f.GetString ("email", "")
                Phone = f.GetString ("phone", "")
                SquirrelName = f.GetString ("squirrelname", "")
            }
        let! squirrelId = inserTrainingRequest dataToSave
        return! Response.ofJson { SquirrelId = squirrelId } ctx
    }