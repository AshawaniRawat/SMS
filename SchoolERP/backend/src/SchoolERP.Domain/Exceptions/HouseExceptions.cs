using SchoolERP.Domain.Enums;namespace SchoolERP.Domain.Exceptions;
public sealed class InvalidHouseStatusTransitionException(HouseStatus from,HouseStatus to):DomainException($"House.InvalidStatusTransition: {from} to {to}");
public sealed class HouseArchivedReadOnlyException():DomainException("House.ArchivedReadOnly");
