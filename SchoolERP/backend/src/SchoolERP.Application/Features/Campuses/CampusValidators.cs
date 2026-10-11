using FluentValidation;
namespace SchoolERP.Application.Features.Campuses;

public sealed class CampusProfileRequestValidator : AbstractValidator<CampusProfileRequest>
{
    public CampusProfileRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200); RuleFor(x => x.ShortName).MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(320).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(30); RuleFor(x => x.AlternatePhone).MaximumLength(30);
        RuleFor(x => x.AddressLine1).MaximumLength(200); RuleFor(x => x.AddressLine2).MaximumLength(200);
        RuleFor(x => x.City).MaximumLength(100); RuleFor(x => x.State).MaximumLength(100); RuleFor(x => x.PostalCode).MaximumLength(20);
        RuleFor(x => x.CountryCode).Length(2).When(x => !string.IsNullOrWhiteSpace(x.CountryCode)); RuleFor(x => x.TimeZoneId).MaximumLength(100);
        RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m).When(x => x.Longitude.HasValue);
        RuleFor(x => x).Must(x => x.Latitude.HasValue == x.Longitude.HasValue)
            .WithMessage("Latitude and longitude must either both be supplied or both be empty.");
    }
}
public sealed class CreateCampusRequestValidator : AbstractValidator<CreateCampusRequest>
{
    public CreateCampusRequestValidator() { Include(new CampusProfileRequestValidator()); RuleFor(x => x.Code).NotEmpty().MaximumLength(50).Matches("^[A-Za-z0-9]+(?:[-_][A-Za-z0-9]+)*$"); }
}
public sealed class ChangeCampusCodeRequestValidator : AbstractValidator<ChangeCampusCodeRequest>
{
    public ChangeCampusCodeRequestValidator() => RuleFor(x => x.Code).NotEmpty().MaximumLength(50).Matches("^[A-Za-z0-9]+(?:[-_][A-Za-z0-9]+)*$");
}
