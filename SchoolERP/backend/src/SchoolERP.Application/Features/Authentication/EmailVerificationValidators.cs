using FluentValidation;

namespace SchoolERP.Application.Features.Authentication;

public sealed class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
    public ConfirmEmailRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Token).NotEmpty().MaximumLength(8192);
    }
}

public sealed class ResendEmailVerificationRequestValidator : AbstractValidator<ResendEmailVerificationRequest>
{
    public ResendEmailVerificationRequestValidator() =>
        RuleFor(x => x.Email).NotEmpty().MaximumLength(320).EmailAddress();
}
