namespace SchoolERP.Application.Exceptions;

public sealed class TenantNotOperationalException : Exception
{
    public TenantNotOperationalException() : base("The tenant is suspended or archived and cannot manage schools.") { }
}
