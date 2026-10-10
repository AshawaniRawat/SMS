namespace SchoolERP.Application.Abstractions;
public interface IClientRequestContext
{
    string? IpAddress { get; }
    string? UserAgent { get; }
}
