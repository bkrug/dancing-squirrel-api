module CreateTrainingRequestTests

open System.Threading.Tasks
open Falco
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
         Task.FromResult( Ok getGenericSuccess)

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

      submissionResult.IsOk.ShouldBeTrue()
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
         Task.FromResult( Ok getGenericSuccess)

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

      submissionResult.IsOk.ShouldBeTrue()
      actualRecievedForm.ShouldBeEquivalentTo(Some expectedForm)
   }

[<Theory>]
[<InlineData("1(212)555-1234", "12125551234")>]
[<InlineData("(212)555-1234", "2125551234")>]
[<InlineData("212-555-1234", "2125551234")>]
[<InlineData("1-212-555-1234", "12125551234")>]
[<InlineData("1  212 5551234 ", "12125551234")>]
[<InlineData("2125551234", "2125551234")>]
let ``Phone numbers may omit or not omit the international code. Expect a success response.`` inputPhoneNumber persistedPhoneNumber =
   task {
      let formValues = [
            "caretakertype", RNumber (int32 CaretakerType.Person)
            "caretakerCompanyName", RNull
            "caretakerFirstName", RString "Betty"
            "caretakerLastName", RString "Blue"
            "email", RString "acme@example.com"
            "phone", RString inputPhoneNumber
            "squirrelname", RString "Nutty"
            "descriptionOfNeeds", RString "Dancing will give this squirrel a more rewarding life"
         ]
      let formData = new FormData(RObject formValues, None)

      let mutable actualRecievedForm : Option<TrainingRequestForm> = None
      let (insertRec:TrainingRequestFormInserter<'a>) = fun form ->
         actualRecievedForm <- Some form
         Task.FromResult( Ok getGenericSuccess)

      //Act
      let! submissionResult = createTrainingRequestFromForm formData insertRec

      //Assert
      let expectedForm = {
            CaretakerType = CaretakerType.Person
            CaretakerCompanyName = None
            CaretakerFirstName = Some "Betty"
            CaretakerLastName = Some "Blue"
            Email = "acme@example.com"
            Phone = persistedPhoneNumber
            SquirrelName = "Nutty"
            DescriptionOfNeeds = "Dancing will give this squirrel a more rewarding life"
         }

      submissionResult.IsOk.ShouldBeTrue()
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
      submissionResult.IsError.ShouldBeTrue()
      actualRecievedForm.ShouldNotBeNull() |> ignore
   }

let validationFailureData =
   [
      (
         [
            "caretakertype", RNumber (int32 CaretakerType.Company)
            "caretakerCompanyName", RString ""
            "caretakerFirstName", RNull
            "caretakerLastName", RNull
            "email", RString "acme@example.com"
            "phone", RString "1-414-555-2983"
            "squirrelname", RString "Nutty"
            "descriptionOfNeeds", RString "Dancing will give this squirrel a more rewarding life"
         ],
         "CaretakerCompanyName",
         "is required"
      )
      (
         [
            "caretakertype", RNumber (int32 CaretakerType.Person)
            "caretakerCompanyName", RNull
            "caretakerFirstName", RNull
            "caretakerLastName", RString "Smith"
            "email", RString "acme@example.com"
            "phone", RString "1-414-555-2983"
            "squirrelname", RString "Nutty"
            "descriptionOfNeeds", RString "Dancing will give this squirrel a more rewarding life"
         ],
         "CaretakerFirstName",
         "is required"
      )
      (
         [
            "caretakertype", RNumber (int32 CaretakerType.Person)
            "caretakerCompanyName", RNull
            "caretakerFirstName", RString "John"
            "caretakerLastName", RString ""
            "email", RString "acme@example.com"
            "phone", RString "1-414-555-2983"
            "squirrelname", RString "Nutty"
            "descriptionOfNeeds", RString "Dancing will give this squirrel a more rewarding life"
         ],
         "CaretakerLastName",
         "is required"
      )
      (
         [
            "caretakertype", RNumber (int32 CaretakerType.Person)
            "caretakerCompanyName", RNull
            "caretakerFirstName", RString "Josie"
            "caretakerLastName", RString "Cat"
            "email", RString ""
            "phone", RString "1-414-555-2983"
            "squirrelname", RString "Nutty"
            "descriptionOfNeeds", RString "Dancing will give this squirrel a more rewarding life"
         ],
         "Email",
         "is required"
      )
      (
         [
            "caretakertype", RNumber (int32 CaretakerType.Person)
            "caretakerCompanyName", RNull
            "caretakerFirstName", RString "Josie"
            "caretakerLastName", RString "Cat"
            "email", RString "b@a.com"
            "phone", RString "9-414-555-2983"
            "squirrelname", RString "Nutty"
            "descriptionOfNeeds", RString "Dancing will give this squirrel a more rewarding life"
         ],
         "Phone",
         "must either have exactly 10 digits or a '1' followed by 10 digits"
      )
      (
         [
            "caretakertype", RNumber (int32 CaretakerType.Person)
            "caretakerCompanyName", RNull
            "caretakerFirstName", RString "Josie"
            "caretakerLastName", RString "Cat"
            "email", RString "b@a.com"
            "phone", RString "1i414i555i2983"
            "squirrelname", RString "Nutty"
            "descriptionOfNeeds", RString "Dancing will give this squirrel a more rewarding life"
         ],
         "Phone",
         "must not contain letters"
      )            
   ]

[<Theory>]
//[<MemberData(nameof(validationFailureData))>]
[<InlineData(0)>]
[<InlineData(1)>]
[<InlineData(2)>]
[<InlineData(3)>]
[<InlineData(4)>]
[<InlineData(5)>]
let ``Training Request is somehow invalid. Expect a validation failure.`` 
   // (formValues:list<string*RequestValue>)
   // (validationField:string)
   // (validationMsg:string) =
   testNumber =
   task {
      let formValues, validationField, validationMsg = validationFailureData[testNumber]

      let formData = new FormData(RObject formValues, None)

      let (insertRec:TrainingRequestFormInserter<'a>) = fun form ->
         Task.FromResult( Ok getGenericSuccess)

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
            errResp.ValidationFailures.Value.GetType()
               .GetProperty(validationField)
               .GetValue(errResp.ValidationFailures.Value)
               .ShouldBeEquivalentTo(validationMsg)
   }
