namespace SchoolERP.Application.Abstractions;

public interface ICurrentTenant
{
    Guid? TenantId { get; }
    bool IsAvailable { get; }
}
