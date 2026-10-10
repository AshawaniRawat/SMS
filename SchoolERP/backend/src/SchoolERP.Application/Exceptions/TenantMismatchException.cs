namespace SchoolERP.Application.Exceptions;

public sealed class TenantMismatchException()
    : InvalidOperationException("The entity does not belong to the current tenant.");
