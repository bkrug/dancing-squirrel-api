module ExternalDependencies

open DbLayer

let getDbContextFactory connStr = Database.QueryContextFactory.Create(connStr, printfn "SQL: %O")