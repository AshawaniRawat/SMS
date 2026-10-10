using SchoolERP.Domain.Enums;

namespace SchoolERP.Domain.Exceptions;

public sealed class InvalidSchoolStatusTransitionException(SchoolStatus from, SchoolStatus to)
    : DomainException($"School status cannot transition from {from} to {to}.");
