using FluentValidation;
using SchoolERP.Domain.Enums;

namespace SchoolERP.Application.Features.Roles;

public sealed class CreateTenantRoleRequestValidator : AbstractValidator<CreateTenantRoleRequest>
{
    public CreateTenantRoleRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100).Matches("^[A-Za-z0-9_]+$");
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Type).IsInEnum().NotEqual(RoleType.Platform);
        RuleFor(x => x.SortOrder).InclusiveBetween(0, 100000).When(x => x.SortOrder.HasValue);
    }
}

public sealed class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.SortOrder).InclusiveBetween(0, 100000).When(x => x.SortOrder.HasValue);
    }
}
