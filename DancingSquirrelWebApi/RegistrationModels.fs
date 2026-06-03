module Registration.Models

type CreateUserModel = 
    {
        Username: string
        //TODO: Better practice is to generate a one-time password upon creation. Not accept one from the user.
        Password : string
        Email : string
        PhoneNumber: string
    }

type OwnPasswordResetModel =
    {
        OldPassword: string
        NewPassword: string
    }

type PasswordResetModel =
    {
        Password: string
    }

type EditUserModel =
    {
        Email: string
        PhoneNumber: string
    }

type RoleModel =
    {
        Name: string
    }

type ViewUserModel =
    {
        UserId: string
        Username: string
        Email: string
        PhoneNumber: string
        Roles: seq<RoleModel>
    }

type GridUserModel =
    {
        UserId: string
        Username: string
        Email: string
    }

type RoleEditingModel =
    {
        Roles: seq<RoleModel>
    }