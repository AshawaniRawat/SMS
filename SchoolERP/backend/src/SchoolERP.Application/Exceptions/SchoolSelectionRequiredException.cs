namespace SchoolERP.Application.Exceptions;
public sealed class SchoolSelectionRequiredException() : Exception("A school-scoped account is required when a tenant has multiple schools.");
