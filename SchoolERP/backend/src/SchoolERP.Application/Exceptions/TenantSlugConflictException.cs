namespace SchoolERP.Application.Exceptions;

public sealed class TenantSlugConflictException()
    : InvalidOperationException("A tenant with this slug already exists.");
