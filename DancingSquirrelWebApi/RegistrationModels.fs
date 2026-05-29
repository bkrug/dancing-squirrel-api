module Registration.Models

type RegisterModel = 
    {
        Email : string
        Username: string
        //TODO: Better practice is to generate a one-time password upon creation. Not accept one from the user.
        Password : string
        PhoneNumber: string
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

type ViewRoleModel =
    {
        Name: string
    }

type ViewUserModel =
    {
        UserId: string
        Username: string
        Email: string
        PhoneNumber: string
        Roles: seq<ViewRoleModel>
    }

type GridUserModel =
    {
        UserId: string
        Username: string
        Email: string
    }