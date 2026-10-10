using SchoolERP.Domain.Enums;
namespace SchoolERP.Domain.Exceptions;
public sealed class InvalidAcademicYearStatusTransitionException(AcademicYearStatus from, AcademicYearStatus to)
    : DomainException($"Academic year status cannot transition from {from} to {to}.");
