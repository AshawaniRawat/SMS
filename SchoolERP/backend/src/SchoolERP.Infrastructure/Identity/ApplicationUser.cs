using Microsoft.AspNetCore.Identity;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Exceptions;
using SchoolERP.Domain.Interfaces;

namespace SchoolERP.Infrastructure.Identity;

// Deliberately not ITenantEntity: login/email lookup must include platform and tenant users.
public sealed class ApplicationUser : IdentityUser<Guid>, IAuditableEntity
{
    public Guid? TenantId { get; private set; }
    public Guid? SchoolId { get; private set; }
    public Guid? CampusId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public bool IsActive { get; private set; }
    public UserAccountStatus Status { get; private set; } = UserAccountStatus.Pending;
    public DateTime? LastLoginAt { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public DateTime? LockedAt { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Tenant? Tenant { get; private set; }
    public School? School { get; private set; }
    public Campus? Campus { get; private set; }

    public static ApplicationUser Create(string firstName, string lastName, string email,
        Guid? tenantId = null, Guid? schoolId = null, Guid? campusId = null, bool activate = false,
        string? displayName = null, string? phoneNumber = null, bool emailConfirmed = false)
    {
        ValidateScope(tenantId, schoolId, campusId);
        var normalizedEmail = Required(email, 320, nameof(Email)).ToLowerInvariant();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), Email = normalizedEmail, UserName = normalizedEmail,
            TenantId = tenantId, SchoolId = schoolId, CampusId = campusId,
            EmailConfirmed = emailConfirmed, LockoutEnabled = true
        };
        user.UpdateProfile(firstName, lastName, displayName, phoneNumber);
        if (activate) user.Activate();
        return user;
    }

    public void UpdateProfile(string firstName, string lastName, string? displayName, string? phoneNumber)
    {
        FirstName = Required(firstName, 100, nameof(FirstName));
        LastName = Required(lastName, 100, nameof(LastName));
        DisplayName = Optional(displayName, 200, nameof(DisplayName));
        PhoneNumber = Optional(phoneNumber, 30, nameof(PhoneNumber));
    }

    public void Activate()
    {
        if (Status == UserAccountStatus.Active) return;
        if (Status is not (UserAccountStatus.Pending or UserAccountStatus.Suspended or UserAccountStatus.Locked))
            throw new InvalidUserAccountStatusTransitionException(Status, UserAccountStatus.Active);
        Status = UserAccountStatus.Active; IsActive = true; DeactivatedAt = null; LockedAt = null;
    }

    public void Suspend()
    {
        if (Status == UserAccountStatus.Suspended) return;
        if (Status != UserAccountStatus.Active)
            throw new InvalidUserAccountStatusTransitionException(Status, UserAccountStatus.Suspended);
        Status = UserAccountStatus.Suspended; IsActive = false;
    }

    public void Lock(DateTime occurredOnUtc)
    {
        if (Status == UserAccountStatus.Locked) return;
        if (Status != UserAccountStatus.Active)
            throw new InvalidUserAccountStatusTransitionException(Status, UserAccountStatus.Locked);
        Status = UserAccountStatus.Locked; IsActive = false; LockedAt = Utc(occurredOnUtc);
    }

    public void Deactivate(DateTime occurredOnUtc)
    {
        if (Status == UserAccountStatus.Deactivated) return;
        Status = UserAccountStatus.Deactivated; IsActive = false; DeactivatedAt = Utc(occurredOnUtc);
    }

    public void RecordSuccessfulLogin(DateTime occurredOnUtc) => LastLoginAt = Utc(occurredOnUtc);

    private static void ValidateScope(Guid? tenantId, Guid? schoolId, Guid? campusId)
    {
        if (!tenantId.HasValue && schoolId.HasValue) throw new DomainException("A school-scoped user requires a tenant.");
        if (!schoolId.HasValue && campusId.HasValue) throw new DomainException("A campus-scoped user requires a school.");
    }
    private static string Required(string? value, int max, string property) { var v = value?.Trim() ?? ""; if (v.Length == 0) throw new DomainException($"User {property} is required."); if (v.Length > max) throw new DomainException($"User {property} cannot exceed {max} characters."); return v; }
    private static string? Optional(string? value, int max, string property) { var v = value?.Trim(); if (string.IsNullOrEmpty(v)) return null; if (v.Length > max) throw new DomainException($"User {property} cannot exceed {max} characters."); return v; }
    private static DateTime Utc(DateTime value) => value.Kind switch { DateTimeKind.Utc => value, DateTimeKind.Local => value.ToUniversalTime(), _ => DateTime.SpecifyKind(value, DateTimeKind.Utc) };
}
