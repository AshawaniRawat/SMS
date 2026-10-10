using SchoolERP.Domain.Enums;

namespace SchoolERP.Domain.Exceptions;

public sealed class InvalidTenantStatusTransitionException(TenantStatus from, TenantStatus to)
    : DomainException($"Tenant status cannot transition from {from} to {to}.");
