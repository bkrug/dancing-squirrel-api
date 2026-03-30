module DbEnv

open DbLayer

type IGetDb =
    abstract member GetDb: unit -> Database.QueryContextFactory

type DbGetter() =
    interface IGetDb with
        member this.GetDb (): Database.QueryContextFactory = 
            let connStr = "Data Source=/home/bkrug/Repos/dancing-squirrel-api/Database/DancingSquirrel.db;"
            let db = Database.QueryContextFactory.Create(connStr, printfn "SQL: %O")
            db