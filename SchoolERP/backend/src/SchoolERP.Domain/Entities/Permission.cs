using System.Text.RegularExpressions;
using SchoolERP.Domain.Common;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Exceptions;
using SchoolERP.Domain.Security;

namespace SchoolERP.Domain.Entities;

public sealed partial class Permission : AuditableEntity
{
    private Permission() { }

    private Permission(PermissionDefinition definition) : base(definition.Id)
    {
        Code = ValidateCode(definition.Code);
        IsSystemPermission = true;
        Synchronize(definition);
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Module { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public PermissionScope Scope { get; private set; }
    public bool IsSystemPermission { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public string? Group { get; private set; }
    public ICollection<RolePermission> RolePermissions { get; } = new List<RolePermission>();

    public static Permission CreateFromDefinition(PermissionDefinition definition) =>
        new(definition ?? throw new ArgumentNullException(nameof(definition)));

    public void Synchronize(PermissionDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (!string.Equals(Code, definition.Code, StringComparison.Ordinal))
            throw new DomainException("A permission code cannot be changed.");
        if (definition.Code != $"{definition.Module}.{definition.Action}")
            throw new DomainException("Permission code must match Module.Action.");

        Name = Required(definition.Name, 200, nameof(Name));
        Module = Required(definition.Module, 100, nameof(Module));
        Action = Required(definition.Action, 100, nameof(Action));
        Description = Optional(definition.Description, 500, nameof(Description));
        if (!Enum.IsDefined(definition.Scope)) throw new DomainException("Permission scope is invalid.");
        if (definition.SortOrder < 0) throw new DomainException("Permission sort order cannot be negative.");
        Scope = definition.Scope;
        SortOrder = definition.SortOrder;
        Group = Optional(definition.Group, 100, nameof(Group));
        IsActive = definition.IsActive;
    }

    public void Deactivate() => IsActive = false;

    private static string ValidateCode(string value)
    {
        var code = Required(value, 150, nameof(Code));
        if (!CodePattern().IsMatch(code))
            throw new DomainException("Permission code must use the PascalCase Module.Action format.");
        return code;
    }

    private static string Required(string? value, int max, string field)
    {
        var result = value?.Trim() ?? string.Empty;
        if (result.Length == 0 || result.Length > max)
            throw new DomainException($"{field} must contain 1 to {max} characters.");
        return result;
    }

    private static string? Optional(string? value, int max, string field)
    {
        var result = value?.Trim();
        if (string.IsNullOrEmpty(result)) return null;
        if (result.Length > max) throw new DomainException($"{field} cannot exceed {max} characters.");
        return result;
    }

    [GeneratedRegex("^[A-Z][A-Za-z0-9]*\\.[A-Z][A-Za-z0-9]*$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();
}
