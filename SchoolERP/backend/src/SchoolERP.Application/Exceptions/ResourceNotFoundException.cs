namespace SchoolERP.Application.Exceptions;

public sealed class ResourceNotFoundException(string resourceName) : Exception($"{resourceName} was not found.");
