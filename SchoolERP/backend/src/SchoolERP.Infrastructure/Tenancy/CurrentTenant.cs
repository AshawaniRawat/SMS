using SchoolERP.Application.Abstractions;

namespace SchoolERP.Infrastructure.Tenancy;

public sealed class CurrentTenant : ICurrentTenant
{
    // TODO: Resolve only from validated identity and tenant membership in a later phase.
    public Guid? TenantId => null;
    public bool IsAvailable => TenantId.HasValue;
}

public sealed class RestrictedPlatformDataAccess : IPlatformDataAccess
{
    public bool IsEnabled => false;
}
