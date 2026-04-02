module SecurityDbLayer

open Microsoft.AspNetCore.Identity.EntityFrameworkCore
open Microsoft.EntityFrameworkCore

type SecurityDbContext(options: DbContextOptions<SecurityDbContext>) =
    inherit IdentityDbContext(options)
