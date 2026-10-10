using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.Identity;

public sealed record CreatePlatformUserRequest(string FirstName, string LastName, string Email, string Password,
    string? DisplayName = null, string? PhoneNumber = null);

public sealed record CreateTenantUserRequest(Guid TenantId, string FirstName, string LastName, string Email, string Password,
    Guid? SchoolId = null, Guid? CampusId = null, string? DisplayName = null, string? PhoneNumber = null);

public sealed record CreateTenantUserApiRequest(string FirstName, string LastName, string Email, string Password,
    Guid? SchoolId = null, Guid? CampusId = null, string? DisplayName = null, string? PhoneNumber = null);

public sealed record UserSummaryDto(Guid Id, string FirstName, string LastName, string Email, string? DisplayName,
    string? PhoneNumber, Guid? TenantId, Guid? SchoolId, Guid? CampusId, UserAccountStatus Status, bool IsActive,
    bool EmailConfirmed, DateTime CreatedAt);

public static class UserRegistrationErrorCodes
{
    public const string Validation = "User.Validation";
    public const string EmailAlreadyExists = "User.EmailAlreadyExists";
    public const string ScopeInvalid = "User.ScopeInvalid";
    public const string ScopeNotFound = "User.ScopeNotFound";
    public const string ScopeNotOperational = "User.ScopeNotOperational";
    public const string IdentityRejected = "User.IdentityRejected";
}
