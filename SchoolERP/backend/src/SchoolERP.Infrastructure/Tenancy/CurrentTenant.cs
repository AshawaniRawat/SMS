using SchoolERP.Application.Abstractions;

namespace SchoolERP.Infrastructure.Tenancy;

public sealed class CurrentTenant(ICurrentUser currentUser) : ICurrentTenant
{
    public Guid? TenantId => currentUser.IsAuthenticated ? currentUser.TenantId : null;
    public bool IsAvailable => TenantId.HasValue;
}

public sealed class RestrictedPlatformDataAccess : IPlatformDataAccess
{
    public bool IsEnabled => false;
}
