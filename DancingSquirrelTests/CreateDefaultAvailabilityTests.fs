module CreateDefaultAvailabilityTests

open System
open System.Threading.Tasks
open Calendar.Endpoints
open Calendar.Models
open Calendar.Queries
open DbLayer.Database.main
open GenericModels
open Shouldly
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
                member _.GetDefaultAvailabilityIdsAsync _ = Task.FromResult([])
                member _.InsertDefaultAvailabilityAsync recordToInsert =
                    actualReceivedRecords <- actualReceivedRecords @ [ recordToInsert ]
                    recordId <- recordId + 1
                    Task.FromResult(Ok recordId)
                member _.UpdateDefaultAvailabilityAsync _ = Task.FromResult(Ok())
                member _.DeleteDefaultAvailabilityAsync _ = Task.FromResult(Ok())
            }

        //Act
        let! submissionResult = crudDefaultAvailabilityFromForm callerInput teacherId fakeQueries

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
                member _.GetDefaultAvailabilityIdsAsync _ = Task.FromResult([])
                member _.InsertDefaultAvailabilityAsync _ = Task.FromResult(Ok -1)
                member _.UpdateDefaultAvailabilityAsync _ = Task.FromResult(Ok())
                member _.DeleteDefaultAvailabilityAsync _ = Task.FromResult(Ok())
            }

        //Act
        let! submissionResult = crudDefaultAvailabilityFromForm callerInput teacherId fakeQueries

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
                member _.GetDefaultAvailabilityIdsAsync _ = Task.FromResult([])
                member _.InsertDefaultAvailabilityAsync _ = Task.FromResult(Ok -1)
                member _.UpdateDefaultAvailabilityAsync _ = Task.FromResult(Ok())
                member _.DeleteDefaultAvailabilityAsync _ = Task.FromResult(Ok())
            }

        //Act
        let! submissionResult = crudDefaultAvailabilityFromForm callerInput teacherId fakeQueries

        //Assert
        match submissionResult with
        | Ok _ -> Assert.Fail "Expected a validation failure"
        | Error errResp ->
            let dayValidation = errResp.ValidationFailures.Value.Availabilities.[2]
            dayValidation.StartTime.ShouldBeEquivalentTo("overlaps another availability period")
    }


let dbErrorFailureCases = TheoryData<DbErrors>(DbErrors.NotFound, DbErrors.ExpectedSingleFoundMultiple)

[<Theory>]
[<MemberData(nameof(dbErrorFailureCases))>]
let ``Editing a default availability when UpdateDefaultAvailabilityAsync fails. Expect the db error to be returned.`` (dbError: DbErrors) =
    task {
        let teacherId = 61
        let callerInput : CreateEditDefaultAvailability =
            {
                Availabilities = [|
                    { DefaultAvailabilityId = Some 500L; DayOfWeek = Some "Monday"; StartTime = Some "09:00:00"; EndTime = Some "17:00:00" }
                |]
            }

        let fakeQueries =
            { new ICalendarQueries with
                member _.BeginTransactionAsync = Task.FromResult()
                member _.CommitTransaction = ()
                member _.GetDefaultAvailabilityAsync _ = Task.FromResult([])
                member _.GetDefaultAvailabilityIdsAsync _ = Task.FromResult([ 500L ])
                member _.InsertDefaultAvailabilityAsync _ = Task.FromResult(Ok -1L)
                member _.UpdateDefaultAvailabilityAsync _ = Task.FromResult(Error dbError)
                member _.DeleteDefaultAvailabilityAsync _ = Task.FromResult(Ok())
            }

        //Act
        let! submissionResult = crudDefaultAvailabilityFromForm callerInput teacherId fakeQueries

        //Assert
        match submissionResult with
        | Ok _ -> Assert.Fail "Expected an error response"
        | Error errResp ->
            let expectedErrResp : GenericModelResponse<DefaultAvailabilityValidation> = getDbErrorsResponse dbError
            errResp.ShouldBeEquivalentTo(expectedErrResp)
    }

[<Theory>]
[<MemberData(nameof(dbErrorFailureCases))>]
let ``Deleting a default availability when DeleteDefaultAvailabilityAsync fails. Expect the db error to be returned.`` (dbError: DbErrors) =
    task {
        let teacherId = 62
        let callerInput : CreateEditDefaultAvailability =
            { Availabilities = [||] }
        let currentDbRecords = [ 700L ]

        let fakeQueries =
            { new ICalendarQueries with
                member _.BeginTransactionAsync = Task.FromResult()
                member _.CommitTransaction = ()
                member _.GetDefaultAvailabilityAsync _ = Task.FromResult([])
                member _.GetDefaultAvailabilityIdsAsync _ = Task.FromResult(currentDbRecords)
                member _.InsertDefaultAvailabilityAsync _ = Task.FromResult(Ok -1L)
                member _.UpdateDefaultAvailabilityAsync _ = Task.FromResult(Ok())
                member _.DeleteDefaultAvailabilityAsync _ = Task.FromResult(Error dbError)
            }

        //Act
        let! submissionResult = crudDefaultAvailabilityFromForm callerInput teacherId fakeQueries

        //Assert
        match submissionResult with
        | Ok _ -> Assert.Fail "Expected an error response"
        | Error errResp ->
            let expectedErrResp : GenericModelResponse<DefaultAvailabilityValidation> = getDbErrorsResponse dbError
            errResp.ShouldBeEquivalentTo(expectedErrResp)
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
        let expectedOutput : ViewDefaultAvailability =
            {
                Availabilities = [|
                    { DefaultAvailabilityId = 3001; DayOfWeek = "Monday";    StartTime = "09:00:00"; EndTime = "17:00:00" }
                    { DefaultAvailabilityId = 1001; DayOfWeek = "Tuesday";   StartTime = "09:00:00"; EndTime = "17:00:00" }
                    { DefaultAvailabilityId = 1002; DayOfWeek = "Wednesday"; StartTime = "09:00:00"; EndTime = "17:00:00" }
                    { DefaultAvailabilityId = 3002; DayOfWeek = "Thursday";  StartTime = "09:00:00"; EndTime = "17:00:00" }
                    { DefaultAvailabilityId = 3003; DayOfWeek = "Saturday";  StartTime = "10:00:00"; EndTime = "14:00:00" }
                |]
            }            
        let currentDbRecords =
            [|
                { DayOfWeek = int64 DayOfWeek.Tuesday;   StartTimeUnix = getUnixSeconds  9 30; EndTimeUnix = getUnixSeconds 17  1; DefaultAvailabilityId = 1001; TeacherId = teacherId; }
                { DayOfWeek = int64 DayOfWeek.Wednesday; StartTimeUnix = getUnixSeconds  9 30; EndTimeUnix = getUnixSeconds 17  2; DefaultAvailabilityId = 1002; TeacherId = teacherId; }
                { DayOfWeek = int64 DayOfWeek.Friday;    StartTimeUnix = getUnixSeconds  8 45; EndTimeUnix = getUnixSeconds 16 45; DefaultAvailabilityId = 2003; TeacherId = teacherId; }
                { DayOfWeek = int64 DayOfWeek.Sunday;    StartTimeUnix = getUnixSeconds  9 15; EndTimeUnix = getUnixSeconds 17 15; DefaultAvailabilityId = 2004; TeacherId = teacherId; }
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
        let expectedDeletes = [| 2003L; 2004L |] |> Seq.toList

        let mutable actualInsertedRecords : list<DefaultAvailability> = []
        let mutable actualUpdatedRecords : list<DefaultAvailability> = []
        let mutable actualDeletedRecords : list<int64> = []
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
                member _.GetDefaultAvailabilityIdsAsync givenTeacherId = 
                    givenTeacherId.ShouldBe(teacherId)
                    Task.FromResult(currentDbRecords |> Seq.map (fun r -> r.DefaultAvailabilityId ))
                member _.InsertDefaultAvailabilityAsync recordToInsert =
                    actualInsertedRecords <- actualInsertedRecords @ [ recordToInsert ]
                    recordId <- recordId + 1
                    Task.FromResult(Ok recordId)
                member _.UpdateDefaultAvailabilityAsync recordToUpdate =
                    actualUpdatedRecords <- actualUpdatedRecords @ [ recordToUpdate ]
                    Task.FromResult(Ok())
                member _.DeleteDefaultAvailabilityAsync recordId =
                    actualDeletedRecords <- actualDeletedRecords @ [ recordId ]
                    Task.FromResult(Ok())
            }

        //Act
        let! submissionResult = crudDefaultAvailabilityFromForm callerInput teacherId fakeQueries

        //Assert
        match submissionResult with
        | Ok successResp -> successResp.ShouldBeEquivalentTo(expectedOutput)
        | Error errResp -> Assert.Fail "Expected a success response"

        actualInsertedRecords.ShouldBeEquivalentTo(expectedInserts)
        actualUpdatedRecords.ShouldBeEquivalentTo(expectedUpdates)
        actualDeletedRecords.ShouldBeEquivalentTo(expectedDeletes)
        callBeginTransactions.ShouldBe(1)
        callCommitTransactions.ShouldBe(1)
    }