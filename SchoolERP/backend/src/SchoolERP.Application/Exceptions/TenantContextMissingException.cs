namespace SchoolERP.Application.Exceptions;

public sealed class TenantContextMissingException()
    : InvalidOperationException("A validated tenant context is required for this operation.");
