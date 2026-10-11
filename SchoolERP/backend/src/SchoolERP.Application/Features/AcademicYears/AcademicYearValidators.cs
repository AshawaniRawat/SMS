using FluentValidation;
using SchoolERP.Domain.Entities;
namespace SchoolERP.Application.Features.AcademicYears;

public sealed class CreateAcademicYearRequestValidator : AbstractValidator<CreateAcademicYearRequest>
{
    public CreateAcademicYearRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50).Matches("^[A-Za-z0-9]+(?:[-_][A-Za-z0-9]+)*$");
        RuleFor(x => x.StartDate).NotEqual(default(DateOnly)); RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
        RuleFor(x => x.EndDate).Must((x, end) => x.StartDate == default || end <= x.StartDate.AddMonths(AcademicYear.MaximumDurationMonths)).WithMessage("Academic year cannot exceed 24 months.");
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x).Must(x => !x.AdmissionStartDate.HasValue || !x.AdmissionEndDate.HasValue || x.AdmissionEndDate >= x.AdmissionStartDate).WithMessage("AdmissionEndDate cannot be before AdmissionStartDate.");
        RuleFor(x => x).Must(x => !x.LockDate.HasValue || (x.LockDate >= x.StartDate && x.LockDate <= x.EndDate)).WithMessage("LockDate must be within the academic year.");
    }
}
public sealed class UpdateAcademicYearRequestValidator : AbstractValidator<UpdateAcademicYearRequest>
{
    public UpdateAcademicYearRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100); RuleFor(x => x.Code).NotEmpty().MaximumLength(50).Matches("^[A-Za-z0-9]+(?:[-_][A-Za-z0-9]+)*$");
        RuleFor(x => x.StartDate).NotEqual(default(DateOnly)); RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
        RuleFor(x => x.Description).MaximumLength(1000); RuleFor(x => x).Must(x => !x.AdmissionStartDate.HasValue || !x.AdmissionEndDate.HasValue || x.AdmissionEndDate >= x.AdmissionStartDate);
        RuleFor(x => x).Must(x => !x.LockDate.HasValue || (x.LockDate >= x.StartDate && x.LockDate <= x.EndDate));
    }
}
public sealed class OpenAdmissionsRequestValidator : AbstractValidator<OpenAdmissionsRequest>
{ public OpenAdmissionsRequestValidator() => RuleFor(x => x).Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.EndDate >= x.StartDate).WithMessage("Admission end date cannot be before start date."); }
