module ExternalDependencies

open DbLayer

type IGetDb =
    abstract member GetDb: unit -> Database.QueryContextFactory

type DbGetter(connStr) =
    interface IGetDb with
        member this.GetDb (): Database.QueryContextFactory = 
            let db = Database.QueryContextFactory.Create(connStr, printfn "SQL: %O")
            db

let getDbContextFactory connStr = Database.QueryContextFactory.Create(connStr, printfn "SQL: %O")