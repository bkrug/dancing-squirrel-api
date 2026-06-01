module UserTests

open GenericModels
open Registration.Models
open Registration.Endpoints
open Shouldly
open Xunit

[<Fact>]
let ``CreateUserModel is valid. Expect a success response.`` () =
  let model : CreateUserModel =
    {
      Email = "user@example.com"
      Username = "testuser"
      Password = "Password123!"
      PhoneNumber = ""
    }

  //Act
  let result = validateCreateUserModel model

  //Assert
  result.IsOk.ShouldBeTrue()

let validationFailureData : list<CreateUserModel * string * string> =
  [
    (
      { Email = "user@example.com"; Username = ""; Password = "Password123!"; PhoneNumber = "" },
      "Username",
      "is required"
    )
    (
      { Email = "user@example.com"; Username = "testuser"; Password = ""; PhoneNumber = "" },
      "Password",
      "is required"
    )
    (
      { Email = ""; Username = "testuser"; Password = "Password123!"; PhoneNumber = "" },
      "Email",
      "is required"
    )
    (
      { Email = "not-an-email"; Username = "testuser"; Password = "Password123!"; PhoneNumber = "" },
      "Email",
      "must be an email address"
    )
    (
      { Email = "user@example.com"; Username = "testuser"; Password = "Password123!"; PhoneNumber = "9-414-555-2983" },
      "PhoneNumber",
      "must either have exactly 10 digits or a '1' followed by 10 digits"
    )
    (
      { Email = "user@example.com"; Username = "testuser"; Password = "Password123!"; PhoneNumber = "1i414i555i2983" },
      "PhoneNumber",
      "must not contain letters"
    )
  ]

[<Theory>]
[<InlineData(0)>]
[<InlineData(1)>]
[<InlineData(2)>]
[<InlineData(3)>]
[<InlineData(4)>]
[<InlineData(5)>]
let ``CreateUserModel is somehow invalid. Expect a validation failure.`` testNumber =
  let model, validationField, validationMsg = validationFailureData[testNumber]

  //Act
  let result = validateCreateUserModel model

  //Asert
  match result with
    | Ok _ -> failwith "Expected a validation failure"
    | Error errResp ->
      errResp.ValidationFailures.IsSome.ShouldBeTrue()
      errResp.ValidationFailures.Value.GetType()
        .GetProperty(validationField)
        .GetValue(errResp.ValidationFailures.Value)
        .ShouldBeEquivalentTo(validationMsg)

[<Fact>]
let ``All fields on CreateUserModel are invalid. Expect validation failures on all fields.`` () =
  let model = { Email = "notAnEmail!"; Username = null; Password = ""; PhoneNumber = "414" }
  let expectedValidationFailures = {
    Email = "must be an email address";
    Username = "is required";
    Password = "is required";
    PhoneNumber = "must either have exactly 10 digits or a '1' followed by 10 digits"
  }

  //Act
  let result = validateCreateUserModel model

  //Assert
  match result with
    | Ok _ -> failwith "Expected a validation failure"
    | Error errResp ->
      errResp.ValidationFailures.IsSome.ShouldBeTrue()
      errResp.ValidationFailures.Value.ShouldBeEquivalentTo(expectedValidationFailures)
