using Microsoft.AspNetCore.Identity;
using SchoolERP.Domain.Interfaces;

namespace SchoolERP.Infrastructure.Identity;

public sealed class ApplicationRole : IdentityRole<Guid>, IAuditableEntity
{
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
