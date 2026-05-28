module Registration.Models

type RegisterModel = 
    {
        Email : string
        Username: string
        //TODO: Better practice is to generate a one-time password upon creation. Not accept one from the user.
        Password : string
        PhoneNumber: string
    }

type LoginModel =
    {
        Username: string
        Password: string
    }

type UnlockUserModel =
    {
        Password: string
    }

type EditUserModel =
    {
        Email: string
        PhoneNumber: string
    }

type ViewUserModel =
    {
        Username: string
        Email: string
        PhoneNumber: string
    }