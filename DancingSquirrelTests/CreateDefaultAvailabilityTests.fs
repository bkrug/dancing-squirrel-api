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

type DefaultAvailabilityUpserter = DefaultAvailability[] -> Task<Result<DefaultAvailability, RecordInsertError>>

let getUnixSeconds hour minute = hour*60*60 + minute*60

[<Fact>]
let ``Default availability form has entries for Monday through Thursday and Saturday. Expect a success response.`` () =
    task {
        let teacherId = 42L
        let callerInput : CreateEditDefaultAvailability =
            {
                Availabilities = [|
                    { TeacherId = teacherId; DayOfWeek = "Monday"; StartTime = "09:00:00"; EndTime = "17:00:00" }
                    { TeacherId = teacherId; DayOfWeek = "Tuesday"; StartTime = "09:00:00"; EndTime = "17:00:00" }
                    { TeacherId = teacherId; DayOfWeek = "Wednesday"; StartTime = "09:00:00"; EndTime = "17:00:00" }
                    { TeacherId = teacherId; DayOfWeek = "Thursday"; StartTime = "09:00:00"; EndTime = "17:00:00" }
                    { TeacherId = teacherId; DayOfWeek = "Saturday"; StartTime = "10:00:00"; EndTime = "14:00:00" }
                |]
            }
        let expectedRecords : DefaultAvailability[] =
            [|
                { TeacherId = teacherId; DayOfWeek = int64 DayOfWeek.Monday;    StartTimeUnix = getUnixSeconds  9 0; EndTimeUnix = getUnixSeconds 17 0; DefaultAvailabilityId = 0; }
                { TeacherId = teacherId; DayOfWeek = int64 DayOfWeek.Tuesday;   StartTimeUnix = getUnixSeconds  9 0; EndTimeUnix = getUnixSeconds 17 0; DefaultAvailabilityId = 0; }
                { TeacherId = teacherId; DayOfWeek = int64 DayOfWeek.Wednesday; StartTimeUnix = getUnixSeconds  9 0; EndTimeUnix = getUnixSeconds 17 0; DefaultAvailabilityId = 0; }
                { TeacherId = teacherId; DayOfWeek = int64 DayOfWeek.Thursday;  StartTimeUnix = getUnixSeconds  9 0; EndTimeUnix = getUnixSeconds 17 0; DefaultAvailabilityId = 0; }
                { TeacherId = teacherId; DayOfWeek = int64 DayOfWeek.Saturday;  StartTimeUnix = getUnixSeconds 10 0; EndTimeUnix = getUnixSeconds 14 0; DefaultAvailabilityId = 0; }
            |]

        let mutable actualReceivedRecords : DefaultAvailability[] option = None
        let (upsertRecord: DefaultAvailabilityUpserter) = fun records ->
            actualReceivedRecords <- Some records
            Task.FromResult(Ok records[0])

        //Act
        let! submissionResult = createDefaultAvailabilityFromForm callerInput upsertRecord

        //Assert
        submissionResult.IsOk.ShouldBeTrue()
        actualReceivedRecords.IsSome.ShouldBeTrue()
        actualReceivedRecords.Value.Length.ShouldBe(expectedRecords.Length)
        actualReceivedRecords.ShouldBeEquivalentTo(expectedRecords)
    }
