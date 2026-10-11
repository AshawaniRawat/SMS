using FluentValidation;

namespace SchoolERP.Application.Features.Authentication;

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator() => RuleFor(x => x.Email).NotEmpty().MaximumLength(320).EmailAddress();
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(320).EmailAddress();
        RuleFor(x => x.Token).NotEmpty().MaximumLength(8192);
        RuleFor(x => x.NewPassword).NotEmpty();
        RuleFor(x => x.ConfirmPassword).NotEmpty().Equal(x => x.NewPassword)
            .WithMessage("Password confirmation must match the new password.");
    }
}
