namespace SchoolERP.Application.Exceptions;

public sealed class ConcurrencyConflictException(Exception innerException)
    : InvalidOperationException("The record was changed by another operation. Reload it and retry.", innerException);
