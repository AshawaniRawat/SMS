using FluentValidation;

namespace SchoolERP.Application.Features.Identity;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(320).EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x).Must(x => x.TenantId.HasValue || (!x.SchoolId.HasValue && !x.CampusId.HasValue))
            .WithMessage("A school or campus assignment requires a tenant.");
        RuleFor(x => x).Must(x => x.SchoolId.HasValue || !x.CampusId.HasValue)
            .WithMessage("A campus assignment requires a school.");
    }
}
