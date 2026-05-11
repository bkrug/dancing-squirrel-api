module Tests

open System
open Xunit
open FsUnit.Xunit

[<Fact>]
let ``My test`` () =
    true |> should equal true
