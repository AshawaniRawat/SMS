using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Exceptions;
using SchoolERP.Application.Features.Schools;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.SchoolProfile;

public sealed class SchoolProfileService(
    IApplicationDbContext context,
    ICurrentSchoolResolver currentSchool,
    IValidator<UpdateSchoolProfileRequest> validator,
    TimeProvider timeProvider) : ISchoolProfileService
{
    public async Task<SchoolProfileDto> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var schoolId = await currentSchool.ResolveAsync(cancellationToken);
        return await context.Schools.AsNoTracking().Where(x => x.Id == schoolId)
            .Select(x => new SchoolProfileDto(x.Id, x.Name, x.Code, x.ShortName, x.RegistrationNumber,
                x.AffiliationNumber, x.BoardCode, x.BoardType, x.Email, x.Phone, x.AlternatePhone,
                x.Website, x.AddressLine1, x.AddressLine2, x.City, x.State, x.PostalCode, x.CountryCode,
                x.LogoUrl, x.TimeZoneId, x.DefaultCurrency, x.PrincipalName, x.Status, x.CreatedAt, x.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken) ?? throw new ResourceNotFoundException("School");
    }

    public async Task<SchoolProfileDto> UpdateCurrentAsync(UpdateSchoolProfileRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var schoolId = await currentSchool.ResolveAsync(cancellationToken);
        var school = await context.Schools.SingleOrDefaultAsync(x => x.Id == schoolId, cancellationToken)
            ?? throw new ResourceNotFoundException("School");
        if (school.Status == SchoolStatus.Archived) throw new SchoolArchivedReadOnlyException();

        school.UpdateProfile(request.Name, request.ShortName, request.RegistrationNumber,
            request.AffiliationNumber, request.BoardCode, request.BoardType, request.Email,
            request.Phone, request.AlternatePhone, request.Website, request.AddressLine1,
            request.AddressLine2, request.City, request.State, request.PostalCode,
            request.CountryCode, school.LogoUrl, request.TimeZoneId, request.DefaultCurrency,
            request.PrincipalName, timeProvider.GetUtcNow().UtcDateTime);
        await context.SaveChangesAsync(cancellationToken);
        return Map(school);
    }

    private static SchoolProfileDto Map(School x) => new(x.Id, x.Name, x.Code, x.ShortName,
        x.RegistrationNumber, x.AffiliationNumber, x.BoardCode, x.BoardType, x.Email, x.Phone,
        x.AlternatePhone, x.Website, x.AddressLine1, x.AddressLine2, x.City, x.State, x.PostalCode,
        x.CountryCode, x.LogoUrl, x.TimeZoneId, x.DefaultCurrency, x.PrincipalName, x.Status,
        x.CreatedAt, x.UpdatedAt);
}
