module CreateDefaultAvailabilityTests

open System
open System.Threading.Tasks
open Falco
open GenericModels
open DbLayer.Database
open DbLayer.Database.main
open Shouldly
open Calendar.Endpoints
open Calendar.Models
open Calendar.Queries
open Xunit

type DefaultAvailabilityUpserter = list<DefaultAvailability> -> Task<Result<list<DefaultAvailability>, DbErrors>>

let getUnixSeconds hour minute = hour*60*60 + minute*60

[<Fact>]
let ``Creating Default availabilities from form with entries for Monday through Thursday and Saturday. Expect a success response.`` () =
    task {
        let teacherId = 42
        let callerInput : CreateEditDefaultAvailability =
            {
                Availabilities = [|
                    { DefaultAvailabilityId = None; DayOfWeek = Some "Monday";    StartTime = Some "09:00:00"; EndTime = Some "17:00:00" }
                    { DefaultAvailabilityId = None; DayOfWeek = Some "Tuesday";   StartTime = Some "09:00:00"; EndTime = Some "17:00:00" }
                    { DefaultAvailabilityId = None; DayOfWeek = Some "WEDNESDAY"; StartTime = Some "09:00:00"; EndTime = Some "17:00:00" }
                    { DefaultAvailabilityId = None; DayOfWeek = Some "thursday";  StartTime = Some "09:00:00"; EndTime = Some "17:00:00" }
                    { DefaultAvailabilityId = None; DayOfWeek = Some "SaturDay";  StartTime = Some "10:00:00"; EndTime = Some "14:00:00" }
                |]
            }
        let expectedRecords =
            [|
                { TeacherId = teacherId; DayOfWeek = int64 DayOfWeek.Monday;    StartTimeUnix = getUnixSeconds  9 0; EndTimeUnix = getUnixSeconds 17 0; DefaultAvailabilityId = 0; }
                { TeacherId = teacherId; DayOfWeek = int64 DayOfWeek.Tuesday;   StartTimeUnix = getUnixSeconds  9 0; EndTimeUnix = getUnixSeconds 17 0; DefaultAvailabilityId = 0; }
                { TeacherId = teacherId; DayOfWeek = int64 DayOfWeek.Wednesday; StartTimeUnix = getUnixSeconds  9 0; EndTimeUnix = getUnixSeconds 17 0; DefaultAvailabilityId = 0; }
                { TeacherId = teacherId; DayOfWeek = int64 DayOfWeek.Thursday;  StartTimeUnix = getUnixSeconds  9 0; EndTimeUnix = getUnixSeconds 17 0; DefaultAvailabilityId = 0; }
                { TeacherId = teacherId; DayOfWeek = int64 DayOfWeek.Saturday;  StartTimeUnix = getUnixSeconds 10 0; EndTimeUnix = getUnixSeconds 14 0; DefaultAvailabilityId = 0; }
            |]
            |> Seq.toList

        let mutable actualReceivedRecords : list<DefaultAvailability> = []
        let mutable recordId = 100
        let mutable callBeginTransactions = 0
        let mutable callCommitTransactions = 0

        let fakeQueries =
            { new ICalendarQueries with
                member _.BeginTransactionAsync =
                    callBeginTransactions <- callBeginTransactions + 1
                    Task.FromResult()
                member _.CommitTransaction = callCommitTransactions <- callCommitTransactions + 1
                member _.GetDefaultAvailabilityAsync _ = Task.FromResult([])
                member _.InsertDefaultAvailability recordToInsert =
                    actualReceivedRecords <- actualReceivedRecords @ [ recordToInsert ]
                    recordId <- recordId + 1
                    Task.FromResult(Ok recordId)
                member _.UpdateDefaultAvailability _ = Task.FromResult(Ok())
                member _.DeleteDefaultAvailability _ = Task.FromResult(Ok())
            }

        //Act
        let! submissionResult = editDefaultAvailabilityFromForm callerInput teacherId fakeQueries

        //Assert
        submissionResult.IsOk.ShouldBeTrue()
        actualReceivedRecords.ShouldBeEquivalentTo(expectedRecords)
        callBeginTransactions.ShouldBe(1)
        callCommitTransactions.ShouldBe(1)
    }

let defaultAvailabilityValidationFailureData : list<CreateEditDefaultDayAvailability * string * string> =
    [
        (
            { DefaultAvailabilityId = None; DayOfWeek = Some "Frunsday"; StartTime = Some "09:00:00"; EndTime = Some "17:00:00" },
            "DayOfWeek",
            "Must be Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, or Sunday"
        )
        (
            { DefaultAvailabilityId = None; DayOfWeek = Some "Tuesday"; StartTime = Some "not-a-time"; EndTime = Some "17:00:00" },
            "StartTime",
            "Must be in the format 'hh:mm'"
        )
        (
            { DefaultAvailabilityId = None; DayOfWeek = Some "Tuesday"; StartTime = Some "09:00:00"; EndTime = Some "not-a-time" },
            "EndTime",
            "Must be in the format 'hh:mm'"
        )
        (
            { DefaultAvailabilityId = None; DayOfWeek = Some ""; StartTime = Some "09:00:00"; EndTime = Some "17:00:00" },
            "DayOfWeek",
            "is required"
        )
        (
            { DefaultAvailabilityId = None; DayOfWeek = Some "Tuesday"; StartTime = Some ""; EndTime = Some "17:00:00" },
            "StartTime",
            "is required"
        )
        (
            { DefaultAvailabilityId = None; DayOfWeek = Some "Tuesday"; StartTime = Some "09:00:00"; EndTime = Some "" },
            "EndTime",
            "is required"
        )
        (
            { DefaultAvailabilityId = None; DayOfWeek = None; StartTime = Some "09:00:00"; EndTime = Some "17:00:00" },
            "DayOfWeek",
            "is required"
        )
        (
            { DefaultAvailabilityId = None; DayOfWeek = Some "Tuesday"; StartTime = None; EndTime = Some "17:00:00" },
            "StartTime",
            "is required"
        )
        (
            { DefaultAvailabilityId = None; DayOfWeek = Some "Tuesday"; StartTime = Some "09:00:00"; EndTime = None },
            "EndTime",
            "is required"
        )
        (
            { DefaultAvailabilityId = None; DayOfWeek = Some "Tuesday"; StartTime = Some "09:00:00"; EndTime = Some "08:00:00" },
            "EndTime",
            "StartTime must precede EndTime"
        )
    ]

[<Theory>]
[<MemberData(nameof(defaultAvailabilityValidationFailureData))>]
let ``Creating default availability that is somehow invalid. Expect a validation failure.``
    (invalidEntry: CreateEditDefaultDayAvailability)
    (validationField: string)
    (validationMsg: string) =
    task {
        let teacherId = 42;
        let callerInput : CreateEditDefaultAvailability =
            {
                Availabilities = [|
                    { DefaultAvailabilityId = None; DayOfWeek = Some "Monday";    StartTime = Some "09:00:00"; EndTime = Some "17:00:00" }
                    invalidEntry
                    { DefaultAvailabilityId = None; DayOfWeek = Some "Wednesday"; StartTime = Some "09:00:00"; EndTime = Some "17:00:00" }
                |]
            }

        let fakeQueries =
            { new ICalendarQueries with
                member _.BeginTransactionAsync = Task.FromResult()
                member _.CommitTransaction = ()
                member _.GetDefaultAvailabilityAsync _ = Task.FromResult([])
                member _.InsertDefaultAvailability _ = Task.FromResult(Ok -1)
                member _.UpdateDefaultAvailability _ = Task.FromResult(Ok())
                member _.DeleteDefaultAvailability _ = Task.FromResult(Ok())
            }

        //Act
        let! submissionResult = editDefaultAvailabilityFromForm callerInput teacherId fakeQueries

        //Assert
        match submissionResult with
        | Ok _ -> Assert.Fail "Expected a validation failure"
        | Error errResp ->
            let dayValidation = errResp.ValidationFailures.Value.Availabilities.[1]
            dayValidation.GetType()
                .GetProperty(validationField)
                .GetValue(dayValidation)
                .ShouldBeEquivalentTo(validationMsg)
    }

[<Fact>]
let ``Creating default availability that has a Wednesday entry that overlaps another Wednesday entry. Expect a validation failure on the overlapping row's StartTime.`` () =
    task {
        let teacherId = 42;
        let callerInput : CreateEditDefaultAvailability =
            {
                Availabilities = [|
                    { DefaultAvailabilityId = None; DayOfWeek = Some "Tuesday";   StartTime = Some "09:00:00"; EndTime = Some "18:00:00" }
                    { DefaultAvailabilityId = None; DayOfWeek = Some "Wednesday"; StartTime = Some "09:00:00"; EndTime = Some "14:00:00" }
                    { DefaultAvailabilityId = None; DayOfWeek = Some "Wednesday"; StartTime = Some "13:00:00"; EndTime = Some "17:00:00" }
                    { DefaultAvailabilityId = None; DayOfWeek = Some "Wednesday"; StartTime = Some "20:00:00"; EndTime = Some "21:00:00" }
                |]
            }

        let fakeQueries =
            { new ICalendarQueries with
                member _.BeginTransactionAsync = Task.FromResult()
                member _.CommitTransaction = ()
                member _.GetDefaultAvailabilityAsync _ = Task.FromResult([])
                member _.InsertDefaultAvailability _ = Task.FromResult(Ok -1)
                member _.UpdateDefaultAvailability _ = Task.FromResult(Ok())
                member _.DeleteDefaultAvailability _ = Task.FromResult(Ok())
            }

        //Act
        let! submissionResult = editDefaultAvailabilityFromForm callerInput teacherId fakeQueries

        //Assert
        match submissionResult with
        | Ok _ -> Assert.Fail "Expected a validation failure"
        | Error errResp ->
            let dayValidation = errResp.ValidationFailures.Value.Availabilities.[2]
            dayValidation.StartTime.ShouldBeEquivalentTo("overlaps another availability period")
    }


[<Fact>]
let ``Editing group of Default availabilities. Expect some records to be inserted, some to be updated, and some to be deleted.`` () =
    task {
        let teacherId = 53
        let callerInput : CreateEditDefaultAvailability =
            {
                Availabilities = [|
                    { DefaultAvailabilityId = None;      DayOfWeek = Some "Monday";    StartTime = Some "09:00:00"; EndTime = Some "17:00:00" }
                    { DefaultAvailabilityId = Some 1001; DayOfWeek = Some "Tuesday";   StartTime = Some "09:00:00"; EndTime = Some "17:00:00" }
                    { DefaultAvailabilityId = Some 1002; DayOfWeek = Some "WEDNESDAY"; StartTime = Some "09:00:00"; EndTime = Some "17:00:00" }
                    { DefaultAvailabilityId = None;      DayOfWeek = Some "thursday";  StartTime = Some "09:00:00"; EndTime = Some "17:00:00" }
                    { DefaultAvailabilityId = None;      DayOfWeek = Some "SaturDay";  StartTime = Some "10:00:00"; EndTime = Some "14:00:00" }
                |]
            }
        let currentDbRecords =
            [|
                { DayOfWeek = int64 DayOfWeek.Tuesday;   StartTimeUnix = getUnixSeconds  9 30; EndTimeUnix = getUnixSeconds 17  0; DefaultAvailabilityId = 1001; TeacherId = teacherId; }
                { DayOfWeek = int64 DayOfWeek.Wednesday; StartTimeUnix = getUnixSeconds  9 30; EndTimeUnix = getUnixSeconds 17  0; DefaultAvailabilityId = 1002; TeacherId = teacherId; }
                { DayOfWeek = int64 DayOfWeek.Wednesday; StartTimeUnix = getUnixSeconds  8 45; EndTimeUnix = getUnixSeconds 16 45; DefaultAvailabilityId = 2003; TeacherId = teacherId; }
                { DayOfWeek = int64 DayOfWeek.Wednesday; StartTimeUnix = getUnixSeconds  9 15; EndTimeUnix = getUnixSeconds 17 15; DefaultAvailabilityId = 2004; TeacherId = teacherId; }
            |]
            |> Seq.toList
        let expectedInserts =
            [|
                { DayOfWeek = int64 DayOfWeek.Monday;    StartTimeUnix = getUnixSeconds  9 0; EndTimeUnix = getUnixSeconds 17 0; DefaultAvailabilityId = 0; TeacherId = teacherId; }
                { DayOfWeek = int64 DayOfWeek.Thursday;  StartTimeUnix = getUnixSeconds  9 0; EndTimeUnix = getUnixSeconds 17 0; DefaultAvailabilityId = 0; TeacherId = teacherId; }
                { DayOfWeek = int64 DayOfWeek.Saturday;  StartTimeUnix = getUnixSeconds 10 0; EndTimeUnix = getUnixSeconds 14 0; DefaultAvailabilityId = 0; TeacherId = teacherId; }
            |]
            |> Seq.toList
        let expectedUpdates =
            [|
                { DayOfWeek = int64 DayOfWeek.Tuesday;   StartTimeUnix = getUnixSeconds  9 0; EndTimeUnix = getUnixSeconds 17 0; DefaultAvailabilityId = 1001; TeacherId = teacherId; }
                { DayOfWeek = int64 DayOfWeek.Wednesday; StartTimeUnix = getUnixSeconds  9 0; EndTimeUnix = getUnixSeconds 17 0; DefaultAvailabilityId = 1002; TeacherId = teacherId; }
            |]
            |> Seq.toList
        let expectedDeletes = [| 2003, 2004 |] |> Seq.toList

        let mutable actualInsertedRecords : list<DefaultAvailability> = []
        let mutable actualUpdatedRecords : list<DefaultAvailability> = []
        let mutable actualDeletedRecords : list<int> = []
        let mutable recordId = 3000
        let mutable callBeginTransactions = 0
        let mutable callCommitTransactions = 0

        let fakeQueries =
            { new ICalendarQueries with
                member _.BeginTransactionAsync =
                    callBeginTransactions <- callBeginTransactions + 1
                    Task.FromResult()
                member _.CommitTransaction =
                    callCommitTransactions <- callCommitTransactions + 1
                member _.GetDefaultAvailabilityAsync givenTeacherId = 
                    givenTeacherId.ShouldBe(teacherId)
                    Task.FromResult(currentDbRecords)
                member _.InsertDefaultAvailability recordToInsert =
                    actualInsertedRecords <- actualInsertedRecords @ [ recordToInsert ]
                    recordId <- recordId + 1
                    Task.FromResult(Ok recordId)
                member _.UpdateDefaultAvailability recordToUpdate =
                    actualUpdatedRecords <- actualUpdatedRecords @ [ recordToUpdate ]
                    Task.FromResult(Ok())
                member _.DeleteDefaultAvailability recordId =
                    actualDeletedRecords <- actualDeletedRecords @ [ recordId ]
                    Task.FromResult(Ok())
            }

        //Act
        let! submissionResult = editDefaultAvailabilityFromForm callerInput teacherId fakeQueries

        //Assert
        submissionResult.IsOk.ShouldBeTrue()
        actualInsertedRecords.ShouldBeEquivalentTo(expectedInserts)
        actualUpdatedRecords.ShouldBeEquivalentTo(expectedUpdates)
        actualDeletedRecords.ShouldBeEquivalentTo(expectedDeletes)
        callBeginTransactions.ShouldBe(1)
        callCommitTransactions.ShouldBe(1)
    }