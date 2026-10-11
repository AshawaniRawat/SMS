using Microsoft.AspNetCore.Identity;
using SchoolERP.Domain.Interfaces;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Exceptions;

namespace SchoolERP.Infrastructure.Identity;

public sealed class ApplicationRole : IdentityRole<Guid>, IAuditableEntity
{
    private ApplicationRole() { }
    public Guid? TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public RoleType Type { get; private set; }
    public bool IsSystemRole { get; private set; }
    public bool IsActive { get; private set; }
    public RoleStatus Status { get; private set; }
    public int? SortOrder { get; private set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public static ApplicationRole CreateTenant(Guid tenantId, string code, string displayName, RoleType type,
        string? description = null, int? sortOrder = null)
    {
        if (tenantId == Guid.Empty) throw new DomainException("A tenant role requires a tenant.");
        if (type == RoleType.Platform) throw new DomainException("A tenant role cannot use Platform scope.");
        var normalizedCode = NormalizeCode(code);
        return new ApplicationRole
        {
            Id = Guid.CreateVersion7(), TenantId = tenantId, Code = normalizedCode,
            Name = BuildTenantName(tenantId, normalizedCode), DisplayName = Required(displayName, 150),
            Description = Optional(description, 500), Type = type, IsSystemRole = false,
            IsActive = true, Status = RoleStatus.Active, SortOrder = sortOrder,
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };
    }

    public static ApplicationRole CreateSystem(Guid id, string code, string displayName, RoleType type,
        string? description, int sortOrder) => new()
        {
            Id = id, TenantId = null, Code = NormalizeCode(code), Name = NormalizeCode(code),
            NormalizedName = NormalizeCode(code), DisplayName = Required(displayName, 150),
            Description = Optional(description, 500), Type = type, IsSystemRole = true,
            IsActive = true, Status = RoleStatus.Active, SortOrder = sortOrder,
            ConcurrencyStamp = $"system-role-{id:N}"
        };

    public void Update(string displayName, string? description, int? sortOrder)
    {
        EnsureMutable(); DisplayName = Required(displayName, 150); Description = Optional(description, 500);
        SortOrder = sortOrder;
    }
    public void Activate()
    {
        EnsureMutable();
        if (Status == RoleStatus.Archived) throw new DomainException("An archived role cannot be activated.");
        Status = RoleStatus.Active; IsActive = true;
    }
    public void Deactivate()
    {
        EnsureMutable();
        if (Status == RoleStatus.Archived) throw new DomainException("An archived role cannot be deactivated.");
        Status = RoleStatus.Inactive; IsActive = false;
    }
    public void Archive()
    {
        EnsureMutable(); Status = RoleStatus.Archived; IsActive = false;
    }

    public static string BuildTenantName(Guid tenantId, string code) => $"TENANT_{tenantId:N}_{NormalizeCode(code)}";
    public static string NormalizeCode(string value)
    {
        var code = Required(value, 100).ToUpperInvariant();
        if (code.Any(x => !(x is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_')))
            throw new DomainException("Role code may contain only A-Z, 0-9, and underscore.");
        return code;
    }
    private void EnsureMutable()
    {
        if (IsSystemRole) throw new DomainException("System roles cannot be modified.");
    }
    private static string Required(string? value, int max)
    {
        var result = value?.Trim() ?? string.Empty;
        if (result.Length == 0 || result.Length > max) throw new DomainException($"Value must contain 1 to {max} characters.");
        return result;
    }
    private static string? Optional(string? value, int max)
    {
        var result = value?.Trim();
        if (string.IsNullOrEmpty(result)) return null;
        if (result.Length > max) throw new DomainException($"Value cannot exceed {max} characters.");
        return result;
    }
}
