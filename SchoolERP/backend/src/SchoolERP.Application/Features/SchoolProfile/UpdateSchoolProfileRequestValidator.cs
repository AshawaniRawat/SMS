using FluentValidation;

namespace SchoolERP.Application.Features.SchoolProfile;

public sealed class UpdateSchoolProfileRequestValidator : AbstractValidator<UpdateSchoolProfileRequest>
{
    public UpdateSchoolProfileRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ShortName).MaximumLength(100);
        RuleFor(x => x.RegistrationNumber).MaximumLength(100);
        RuleFor(x => x.AffiliationNumber).MaximumLength(100);
        RuleFor(x => x.BoardCode).MaximumLength(100);
        RuleFor(x => x.BoardType).IsInEnum().When(x => x.BoardType.HasValue);
        RuleFor(x => x.Email).MaximumLength(320).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.AlternatePhone).MaximumLength(30);
        RuleFor(x => x.Website).MaximumLength(500).Must(BeHttpUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.Website)).WithMessage("Website must be an absolute HTTP or HTTPS URL.");
        RuleFor(x => x.AddressLine1).MaximumLength(200);
        RuleFor(x => x.AddressLine2).MaximumLength(200);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(100);
        RuleFor(x => x.PostalCode).MaximumLength(20);
        RuleFor(x => x.CountryCode).Matches("^[A-Za-z]{2}$").When(x => !string.IsNullOrWhiteSpace(x.CountryCode));
        RuleFor(x => x.TimeZoneId).MaximumLength(100);
        RuleFor(x => x.DefaultCurrency).Matches("^[A-Za-z]{3}$").When(x => !string.IsNullOrWhiteSpace(x.DefaultCurrency));
        RuleFor(x => x.PrincipalName).MaximumLength(200);
    }

    private static bool BeHttpUrl(string? value) => Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
