using FluentValidation;

namespace SchoolERP.Application.Features.Schools;

public sealed class SchoolProfileRequestValidator : AbstractValidator<SchoolProfileRequest>
{
    public SchoolProfileRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ShortName).MaximumLength(100);
        RuleFor(x => x.RegistrationNumber).MaximumLength(100);
        RuleFor(x => x.AffiliationNumber).MaximumLength(100);
        RuleFor(x => x.BoardCode).MaximumLength(50);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(320).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.AlternatePhone).MaximumLength(30);
        RuleFor(x => x.CountryCode).Length(2).When(x => !string.IsNullOrWhiteSpace(x.CountryCode));
        RuleFor(x => x.DefaultCurrency).Length(3).When(x => !string.IsNullOrWhiteSpace(x.DefaultCurrency));
        RuleFor(x => x.Website).MaximumLength(500);
        RuleFor(x => x.AddressLine1).MaximumLength(250);
        RuleFor(x => x.AddressLine2).MaximumLength(250);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(100);
        RuleFor(x => x.PostalCode).MaximumLength(20);
        RuleFor(x => x.LogoUrl).MaximumLength(1000);
        RuleFor(x => x.TimeZoneId).MaximumLength(100);
        RuleFor(x => x.PrincipalName).MaximumLength(200);
    }
}

public sealed class CreateSchoolRequestValidator : AbstractValidator<CreateSchoolRequest>
{
    public CreateSchoolRequestValidator()
    {
        Include(new SchoolProfileRequestValidator());
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches("^[A-Za-z0-9]+(?:[-_][A-Za-z0-9]+)*$");
    }
}
