namespace SchoolERP.Application.Features.SchoolProfile;

public interface ISchoolProfileService
{
    Task<SchoolProfileDto> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task<SchoolProfileDto> UpdateCurrentAsync(UpdateSchoolProfileRequest request, CancellationToken cancellationToken = default);
}
