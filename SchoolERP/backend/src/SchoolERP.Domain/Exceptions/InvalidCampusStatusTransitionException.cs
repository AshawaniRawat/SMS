using SchoolERP.Domain.Enums;
namespace SchoolERP.Domain.Exceptions;
public sealed class InvalidCampusStatusTransitionException(CampusStatus from, CampusStatus to) : DomainException($"Campus status cannot transition from {from} to {to}.");
