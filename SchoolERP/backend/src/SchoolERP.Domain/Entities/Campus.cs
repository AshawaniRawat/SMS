using SchoolERP.Domain.Common;
using SchoolERP.Domain.Exceptions;
using SchoolERP.Domain.Interfaces;

namespace SchoolERP.Domain.Entities;

public sealed class Campus : AuditableEntity, ITenantEntity, ISoftDeletableEntity
{
    private string _name = string.Empty;
    private string _code = string.Empty;

    public Guid TenantId { get; set; }
    public Guid SchoolId { get; set; }
    public string Name { get => _name; set => _name = NormalizeRequired(value, nameof(Name)); }
    public string Code { get => _code; set => _code = NormalizeRequired(value, nameof(Code)).ToUpperInvariant(); }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public School School { get; set; } = null!;

    private static string NormalizeRequired(string? value, string propertyName)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0) throw new DomainException($"Campus {propertyName} is required.");
        return normalized;
    }
}
