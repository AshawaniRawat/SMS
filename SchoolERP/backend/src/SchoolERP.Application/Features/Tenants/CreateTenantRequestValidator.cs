using System.Net.Mail;
using System.Text.RegularExpressions;
using FluentValidation;
using SchoolERP.Domain.Constants;

namespace SchoolERP.Application.Features.Tenants;

public sealed partial class CreateTenantRequestValidator : AbstractValidator<CreateTenantRequest>
{
    public CreateTenantRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Slug).NotEmpty().MaximumLength(100)
            .Must(slug => slug is not null && SlugPattern().IsMatch(slug.Trim().ToLowerInvariant()))
            .WithMessage("Slug must contain lowercase letters, digits, and single hyphen separators only.")
            .Must(slug => slug is not null && !ReservedTenantSlugs.Contains(slug.Trim().ToLowerInvariant()))
            .WithMessage("Slug is reserved.");
        RuleFor(request => request.Email).MaximumLength(320)
            .Must(BeValidEmail).When(request => !string.IsNullOrWhiteSpace(request.Email));
        RuleFor(request => request.Phone).MaximumLength(30);
        RuleFor(request => request.CountryCode).Length(2).When(request => !string.IsNullOrWhiteSpace(request.CountryCode));
        RuleFor(request => request.DefaultCurrency).Length(3).When(request => !string.IsNullOrWhiteSpace(request.DefaultCurrency));
        RuleFor(request => request.TimeZoneId).MaximumLength(100);
    }

    private static bool BeValidEmail(string? email)
    {
        try { return new MailAddress(email!).Address == email!.Trim(); }
        catch (FormatException) { return false; }
    }

    [GeneratedRegex("\\A[a-z0-9]+(-[a-z0-9]+)*\\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex SlugPattern();
}
