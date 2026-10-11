using Microsoft.EntityFrameworkCore;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Exceptions;
using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.Schools;

public sealed class CurrentSchoolResolver(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    ICurrentTenant currentTenant) : ICurrentSchoolResolver
{
    public async Task<Guid> ResolveAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || !currentTenant.IsAvailable ||
            currentTenant.TenantId is not { } tenantId || tenantId == Guid.Empty)
            throw new TenantContextMissingException();

        if (currentUser.SchoolId is { } selectedSchoolId)
        {
            var belongsToTenant = await context.Schools.AsNoTracking()
                .AnyAsync(x => x.Id == selectedSchoolId && x.TenantId == tenantId, cancellationToken);
            if (!belongsToTenant) throw new ResourceNotFoundException("School");
            return selectedSchoolId;
        }

        var schools = await context.Schools.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != SchoolStatus.Archived)
            .Select(x => x.Id).Take(2).ToListAsync(cancellationToken);

        return schools.Count switch
        {
            1 => schools[0],
            0 => throw new ResourceNotFoundException("School"),
            _ => throw new SchoolSelectionRequiredException()
        };
    }
}
