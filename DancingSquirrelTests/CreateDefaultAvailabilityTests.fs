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
open Xunit

type DefaultAvailabilityUpserter = list<DefaultAvailability> -> Task<Result<list<DefaultAvailability>, RecordInsertError>>

let getUnixSeconds hour minute = hour*60*60 + minute*60

[<Fact>]
let ``Default availability form has entries for Monday through Thursday and Saturday. Expect a success response.`` () =
    task {
        let teacherId = 42L
        let callerInput : CreateEditDefaultAvailability =
            {
                Availabilities = [|
                    { TeacherId = teacherId; DayOfWeek = Some "Monday";    StartTime = Some "09:00:00"; EndTime = Some "17:00:00" }
                    { TeacherId = teacherId; DayOfWeek = Some "Tuesday";   StartTime = Some "09:00:00"; EndTime = Some "17:00:00" }
                    { TeacherId = teacherId; DayOfWeek = Some "WEDNESDAY"; StartTime = Some "09:00:00"; EndTime = Some "17:00:00" }
                    { TeacherId = teacherId; DayOfWeek = Some "thursday";  StartTime = Some "09:00:00"; EndTime = Some "17:00:00" }
                    { TeacherId = teacherId; DayOfWeek = Some "SaturDay";  StartTime = Some "10:00:00"; EndTime = Some "14:00:00" }
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

        let mutable actualReceivedRecords : list<DefaultAvailability> option = None
        let (upsertRecord: DefaultAvailabilityUpserter) = fun records ->
            actualReceivedRecords <- Some records
            Task.FromResult(Ok records)

        //Act
        let! submissionResult = createDefaultAvailabilityFromForm callerInput upsertRecord

        //Assert
        submissionResult.IsOk.ShouldBeTrue()
        actualReceivedRecords.IsSome.ShouldBeTrue()
        actualReceivedRecords.Value.ShouldBeEquivalentTo(expectedRecords)
    }

let defaultAvailabilityValidationFailureData : list<CreateEditDefaultDayAvailability * string * string> =
    [
        (
            { TeacherId = 42L; DayOfWeek = Some "Frunsday"; StartTime = Some "09:00:00"; EndTime = Some "17:00:00" },
            "DayOfWeek",
            "Must be Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, or Sunday"
        )
        (
            { TeacherId = 42L; DayOfWeek = Some "Tuesday"; StartTime = Some "not-a-time"; EndTime = Some "17:00:00" },
            "StartTime",
            "Must be in the format 'hh:mm'"
        )
        (
            { TeacherId = 42L; DayOfWeek = Some "Tuesday"; StartTime = Some "09:00:00"; EndTime = Some "not-a-time" },
            "EndTime",
            "Must be in the format 'hh:mm'"
        )
        (
            { TeacherId = 42L; DayOfWeek = Some ""; StartTime = Some "09:00:00"; EndTime = Some "17:00:00" },
            "DayOfWeek",
            "is required"
        )
        (
            { TeacherId = 42L; DayOfWeek = Some "Tuesday"; StartTime = Some ""; EndTime = Some "17:00:00" },
            "StartTime",
            "is required"
        )
        (
            { TeacherId = 42L; DayOfWeek = Some "Tuesday"; StartTime = Some "09:00:00"; EndTime = Some "" },
            "EndTime",
            "is required"
        )
        (
            { TeacherId = 42L; DayOfWeek = None; StartTime = Some "09:00:00"; EndTime = Some "17:00:00" },
            "DayOfWeek",
            "is required"
        )
        (
            { TeacherId = 42L; DayOfWeek = Some "Tuesday"; StartTime = None; EndTime = Some "17:00:00" },
            "StartTime",
            "is required"
        )
        (
            { TeacherId = 42L; DayOfWeek = Some "Tuesday"; StartTime = Some "09:00:00"; EndTime = None },
            "EndTime",
            "is required"
        )
        (
            { TeacherId = 42L; DayOfWeek = Some "Tuesday"; StartTime = Some "09:00:00"; EndTime = Some "08:00:00" },
            "EndTime",
            "StartTime must precede EndTime"
        )        
    ]

[<Theory>]
[<MemberData(nameof(defaultAvailabilityValidationFailureData))>]
let ``Default availability entry is somehow invalid. Expect a validation failure.``
    (invalidEntry: CreateEditDefaultDayAvailability)
    (validationField: string)
    (validationMsg: string) =
    task {
        let callerInput : CreateEditDefaultAvailability =
            {
                Availabilities = [|
                    { TeacherId = 42L; DayOfWeek = Some "Monday";    StartTime = Some "09:00:00"; EndTime = Some "17:00:00" }
                    invalidEntry
                    { TeacherId = 42L; DayOfWeek = Some "Wednesday"; StartTime = Some "09:00:00"; EndTime = Some "17:00:00" }
                |]
            }

        let (upsertRecord: DefaultAvailabilityUpserter) = fun records ->
            Task.FromResult(Ok records)

        //Act
        let! submissionResult = createDefaultAvailabilityFromForm callerInput upsertRecord

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
let ``Default availability form has a Wednesday entry that overlaps another Wednesday entry. Expect a validation failure on the overlapping row's StartTime.`` () =
    task {
        let callerInput : CreateEditDefaultAvailability =
            {
                Availabilities = [|
                    { TeacherId = 42L; DayOfWeek = Some "Tuesday";   StartTime = Some "09:00:00"; EndTime = Some "18:00:00" }
                    { TeacherId = 42L; DayOfWeek = Some "Wednesday"; StartTime = Some "09:00:00"; EndTime = Some "14:00:00" }
                    { TeacherId = 42L; DayOfWeek = Some "Wednesday"; StartTime = Some "13:00:00"; EndTime = Some "17:00:00" }
                    { TeacherId = 42L; DayOfWeek = Some "Wednesday"; StartTime = Some "20:00:00"; EndTime = Some "21:00:00" }
                |]
            }

        let (upsertRecord: DefaultAvailabilityUpserter) = fun records ->
            Task.FromResult(Ok records)

        //Act
        let! submissionResult = createDefaultAvailabilityFromForm callerInput upsertRecord

        //Assert
        match submissionResult with
        | Ok _ -> Assert.Fail "Expected a validation failure"
        | Error errResp ->
            let dayValidation = errResp.ValidationFailures.Value.Availabilities.[2]
            dayValidation.StartTime.ShouldBeEquivalentTo("overlaps another availability period")
    }
