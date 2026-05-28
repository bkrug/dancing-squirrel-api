module Registration.Models

type RegisterModel = 
    {
        Email : string
        Username: string
        //TODO: Better practice is to generate a one-time password upon creation. Not accept one from the user.
        Password : string
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