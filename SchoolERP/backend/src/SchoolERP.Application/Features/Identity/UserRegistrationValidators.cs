using FluentValidation;
using System.Linq.Expressions;

namespace SchoolERP.Application.Features.Identity;

internal static class UserRegistrationValidation
{
    public static void AddCommon<T>(AbstractValidator<T> validator,
        Expression<Func<T, string>> firstName, Expression<Func<T, string>> lastName, Expression<Func<T, string>> email,
        Expression<Func<T, string>> password, Expression<Func<T, string?>> displayName, Expression<Func<T, string?>> phone)
    {
        validator.RuleFor(firstName).NotEmpty().MaximumLength(100);
        validator.RuleFor(lastName).NotEmpty().MaximumLength(100);
        validator.RuleFor(email).NotEmpty().MaximumLength(320).EmailAddress();
        validator.RuleFor(password).NotEmpty();
        validator.RuleFor(displayName).MaximumLength(200);
        validator.RuleFor(phone).MaximumLength(30);
    }
}

public sealed class CreatePlatformUserRequestValidator : AbstractValidator<CreatePlatformUserRequest>
{
    public CreatePlatformUserRequestValidator() => UserRegistrationValidation.AddCommon(this,
        x => x.FirstName, x => x.LastName, x => x.Email, x => x.Password, x => x.DisplayName, x => x.PhoneNumber);
}

public sealed class CreateTenantUserRequestValidator : AbstractValidator<CreateTenantUserRequest>
{
    public CreateTenantUserRequestValidator()
    {
        UserRegistrationValidation.AddCommon(this,
            x => x.FirstName, x => x.LastName, x => x.Email, x => x.Password, x => x.DisplayName, x => x.PhoneNumber);
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x).Must(x => x.SchoolId.HasValue || !x.CampusId.HasValue)
            .WithMessage("A campus assignment requires a school.");
    }
}
