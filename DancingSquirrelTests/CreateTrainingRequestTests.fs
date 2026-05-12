module CreateTrainingRequestTests

open System.Threading.Tasks
open Falco
open FsUnit.Xunit
open GenericModels
open Shouldly
open TrainingRequest.Endpoints
open TrainingRequest.Models
open Xunit

[<Fact>]
let ``Training Request for Company is valid. Expect a success response.`` () =
   task {
      let formValues = [
            "caretakertype", RNumber (int32 CaretakerType.Company)
            "caretakerCompanyName", RString "Acme"
            "caretakerFirstName", RNull
            "caretakerLastName", RNull
            "email", RString "acme@example.com"
            "phone", RString "1-414-555-2983"
            "squirrelname", RString "Nutty"
            "descriptionOfNeeds", RString "Dancing will give this squirrel a more rewarding life"
         ]
      let formData = new FormData(RObject formValues, None)

      let mutable actualRecievedForm : Option<TrainingRequestForm> = None
      let (insertRec:TrainingRequestFormInserter<'a>) = fun form ->
         actualRecievedForm <- Some form
         Task.FromResult( Ok {
            IsSuccess = true
            IsInternalError = false
            ValidationFailures = None
         })

      //Act
      let! submissionResult = createTrainingRequestFromForm formData insertRec

      //Assert
      let expectedForm = {
            CaretakerType = CaretakerType.Company
            CaretakerCompanyName = Some "Acme"
            CaretakerFirstName = None
            CaretakerLastName = None
            Email = "acme@example.com"
            Phone = "14145552983"
            SquirrelName = "Nutty"
            DescriptionOfNeeds = "Dancing will give this squirrel a more rewarding life"
         }

      submissionResult.IsOk |> should equal true
      actualRecievedForm.ShouldBeEquivalentTo(Some expectedForm)
   }

[<Fact>]
let ``Training Request for Person is valid. Expect a success response.`` () =
   task {
      let formValues = [
            "caretakertype", RNumber (int32 CaretakerType.Person)
            "caretakerCompanyName", RNull
            "caretakerFirstName", RString "Betty"
            "caretakerLastName", RString "Blue"
            "email", RString "acme@example.com"
            "phone", RString "1-414-555-2983"
            "squirrelname", RString "Nutty"
            "descriptionOfNeeds", RString "Dancing will give this squirrel a more rewarding life"
         ]
      let formData = new FormData(RObject formValues, None)

      let mutable actualRecievedForm : Option<TrainingRequestForm> = None
      let (insertRec:TrainingRequestFormInserter<'a>) = fun form ->
         actualRecievedForm <- Some form
         Task.FromResult( Ok {
            IsSuccess = true
            IsInternalError = false
            ValidationFailures = None
         })

      //Act
      let! submissionResult = createTrainingRequestFromForm formData insertRec

      //Assert
      let expectedForm = {
            CaretakerType = CaretakerType.Person
            CaretakerCompanyName = None
            CaretakerFirstName = Some "Betty"
            CaretakerLastName = Some "Blue"
            Email = "acme@example.com"
            Phone = "14145552983"
            SquirrelName = "Nutty"
            DescriptionOfNeeds = "Dancing will give this squirrel a more rewarding life"
         }

      submissionResult.IsOk |> should equal true
      actualRecievedForm.ShouldBeEquivalentTo(Some expectedForm)
   }

[<Fact>]
let ``Training Request is valid, but there was some DB Error. Expect a failure response.`` () =
   task {
      let formValues = [
            "caretakertype", RNumber (int32 CaretakerType.Company)
            "caretakerCompanyName", RString "Acme"
            "caretakerFirstName", RNull
            "caretakerLastName", RNull
            "email", RString "acme@example.com"
            "phone", RString "1-414-555-2983"
            "squirrelname", RString "Nutty"
            "descriptionOfNeeds", RString "Dancing will give this squirrel a more rewarding life"
         ]
      let formData = new FormData(RObject formValues, None)

      let mutable actualRecievedForm : Option<TrainingRequestForm> = None
      let (insertRec:TrainingRequestFormInserter<'a>) = fun form ->
         actualRecievedForm <- Some form
         Task.FromResult(Error internalErrorResponse)

      //Act
      let! submissionResult = createTrainingRequestFromForm formData insertRec

      //Assert
      submissionResult.IsError |> should equal true
      actualRecievedForm |> should not' (be null)
   }

[<Fact>]
let ``Training Request for Company but there is no Company Name. Expect a validation failure.`` () =
   task {
      let formValues = [
            "caretakertype", RNumber (int32 CaretakerType.Company)
            "caretakerCompanyName", RNull
            "caretakerFirstName", RNull
            "caretakerLastName", RNull
            "email", RString "acme@example.com"
            "phone", RString "1-414-555-2983"
            "squirrelname", RString "Nutty"
            "descriptionOfNeeds", RString "Dancing will give this squirrel a more rewarding life"
         ]
      let formData = new FormData(RObject formValues, None)

      let (insertRec:TrainingRequestFormInserter<'a>) = fun form ->
         Task.FromResult( Ok {
            IsSuccess = true
            IsInternalError = false
            ValidationFailures = None
         })

      //Act
      let! submissionResult = createTrainingRequestFromForm formData insertRec

      //Assert
      let expectedForm = {
            CaretakerType = CaretakerType.Company
            CaretakerCompanyName = Some "Acme"
            CaretakerFirstName = None
            CaretakerLastName = None
            Email = "acme@example.com"
            Phone = "14145552983"
            SquirrelName = "Nutty"
            DescriptionOfNeeds = "Dancing will give this squirrel a more rewarding life"
         }

      match submissionResult with
         | Ok _ -> failwith "Expected a validation failure"
         | Error errResp ->
            errResp.ValidationFailures.IsSome.ShouldBeTrue()
            errResp.ValidationFailures.Value.CaretakerCompanyName.ShouldBeEquivalentTo("is required")
   }
