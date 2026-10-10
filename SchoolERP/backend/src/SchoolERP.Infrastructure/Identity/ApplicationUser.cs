using Microsoft.AspNetCore.Identity;
namespace SchoolERP.Infrastructure.Identity;
// Register Identity stores and migrations only when authentication is implemented.
public sealed class ApplicationUser : IdentityUser<Guid> { }