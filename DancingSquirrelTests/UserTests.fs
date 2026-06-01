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

  let result = validateCreateUserModel model

  result.IsSuccess.ShouldBeTrue()

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
let ``CreateUserModel is somehow invalid. Expect a validation failure.``
  testNumber =
  let model, validationField, validationMsg = validationFailureData[testNumber]

  //Act
  let result = validateCreateUserModel model

  result.IsSuccess.ShouldBeFalse()
  result.ValidationFailures.IsSome.ShouldBeTrue()
  match result.ValidationFailures.Value with
  | Error validFails ->
    validFails.GetType()
      .GetProperty(validationField)
      .GetValue(validFails)
      .ShouldBeEquivalentTo(validationMsg)
  | Ok _ -> failwith "Expected a validation failure"
