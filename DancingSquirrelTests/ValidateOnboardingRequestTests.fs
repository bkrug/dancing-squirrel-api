module ValidateOnboardingRequestTests

open System.Threading.Tasks
open Falco
open FsUnit.Xunit
open GenericModels
open Shouldly
open TrainingRequest.Endpoints
open TrainingRequest.Models
open Xunit
open DbLayer.Database

[<Fact>]
let ``Training Request does not have a squirrel record associated with it. Expect indication that onboarding is valid.`` () =
    task {
        let trainingRequestRec:main.TrainingRequest =
            {
                TrainingRequestId = 5
                CaretakerType = int64 CaretakerType.Company
                OrganizationName = Some "Acme"
                OwnerFirstName = None
                OwnerLastName = None
                Email = "a@b.com"
                Phone = Some "12125559821"
                SquirrelName = "Fluffy"
                DescriptionOfNeeds = Some "some text"
                //A null squirrelId indicates that the client has not yet been onboarded
                SquirrelId = None
                OnboardUsername = None
                OnboardingDateTimeUnix = None
            }

        //Act
        let! result = validatedOnboardingRequest trainingRequestRec

        //Assert
        result.IsOk.ShouldBeTrue()
    }

[<Fact>]
let ``Client is already onbaorded because training Request has a squirrel record associated with it. Expect indication that onboarding is not valid.`` () =
    task {
        let trainingRequestRec:main.TrainingRequest =
            {
                TrainingRequestId = 382
                CaretakerType = int64 CaretakerType.Company
                OrganizationName = Some "Acme"
                OwnerFirstName = None
                OwnerLastName = None
                Email = "a@b.com"
                Phone = Some "12125559821"
                SquirrelName = "Fluffy"
                DescriptionOfNeeds = Some "some text"
                //A null squirrelId indicates that the client has not yet been onboarded
                SquirrelId = Some 5782
                OnboardUsername = Some "someUser"
                OnboardingDateTimeUnix = Some(System.DateTimeOffset(System.DateTime.UtcNow).ToUnixTimeSeconds())
            }

        //Act
        let! result = validatedOnboardingRequest trainingRequestRec

        //Assert
        match result with
            | Ok _ -> failwith "Expected a validation failure, but found none."
            | Error errObj -> errObj.ValidationFailures.ShouldBe(Some "Caretaker and Squirrel have already been onboarded")        
    }