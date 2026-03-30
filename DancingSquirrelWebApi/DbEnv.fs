module DbEnv

open DbLayer

type IGetDb =
    abstract member GetDb: unit -> Database.QueryContextFactory

type DbGetter(connStr) =
    interface IGetDb with
        member this.GetDb (): Database.QueryContextFactory = 
            let db = Database.QueryContextFactory.Create(connStr, printfn "SQL: %O")
            db