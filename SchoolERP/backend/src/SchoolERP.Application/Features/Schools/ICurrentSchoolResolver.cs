namespace SchoolERP.Application.Features.Schools;

public interface ICurrentSchoolResolver
{
    Task<Guid> ResolveAsync(CancellationToken cancellationToken = default);
}
