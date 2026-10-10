using Microsoft.AspNetCore.Http;
using SchoolERP.Application.Abstractions;
namespace SchoolERP.Infrastructure.Identity;
public sealed class HttpClientRequestContext(IHttpContextAccessor accessor) : IClientRequestContext
{
    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent
    {
        get
        {
            var value = accessor.HttpContext?.Request.Headers.UserAgent.ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(value.Length, 500)];
        }
    }
}
