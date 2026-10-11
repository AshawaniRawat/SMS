using SchoolERP.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace SchoolERP.Infrastructure.Tenancy;

public sealed class CurrentTenant(ICurrentUser currentUser, IPlatformDataAccess platformDataAccess,
    Microsoft.Extensions.Logging.ILogger<CurrentTenant> logger) : ICurrentTenant
{
    public Guid? TenantId
    {
        get
        {
            if (!currentUser.IsAuthenticated) return null;
            if (currentUser.HasMalformedTenantClaim)
                logger.LogWarning("Authenticated user {UserId} supplied a malformed tenant claim.", currentUser.UserId);
            return currentUser.TenantId is { } id && id != Guid.Empty ? id : null;
        }
    }
    public bool IsAvailable => TenantId.HasValue;
    public bool IsPlatformScope => platformDataAccess.IsEnabled;
}

public sealed class RestrictedPlatformDataAccess : IPlatformDataAccess
{
    public bool IsEnabled => false;
}
