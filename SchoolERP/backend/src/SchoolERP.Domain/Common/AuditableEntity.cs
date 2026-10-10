using SchoolERP.Domain.Interfaces;

namespace SchoolERP.Domain.Common;

public abstract class AuditableEntity : Entity, IAuditableEntity
{
    protected AuditableEntity() { }
    protected AuditableEntity(Guid id) : base(id) { }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
