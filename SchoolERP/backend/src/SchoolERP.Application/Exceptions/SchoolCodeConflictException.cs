namespace SchoolERP.Application.Exceptions;

public sealed class SchoolCodeConflictException : Exception
{
    public SchoolCodeConflictException() : base("A school with this code already exists in the tenant.") { }
}
