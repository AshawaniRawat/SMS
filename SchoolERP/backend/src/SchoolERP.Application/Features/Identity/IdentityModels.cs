using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.Identity;

public sealed record CreateUserRequest(string FirstName, string LastName, string Email, string Password,
    Guid? TenantId = null, Guid? SchoolId = null, Guid? CampusId = null, bool ActivateImmediately = false);

public sealed record UserSummaryDto(Guid Id, string FirstName, string LastName, string Email, string? DisplayName,
    Guid? TenantId, Guid? SchoolId, Guid? CampusId, UserAccountStatus Status, bool IsActive,
    bool EmailConfirmed, bool TwoFactorEnabled, DateTime CreatedAt);
