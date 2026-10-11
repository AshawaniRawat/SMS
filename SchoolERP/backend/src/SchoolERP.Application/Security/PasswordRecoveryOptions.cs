using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Application.Security;

public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";
    [Range(5, 1440)] public int TokenLifespanMinutes { get; set; } = 60;
}

public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";
    [Required, Url] public string BaseUrl { get; set; } = "http://localhost:5173";
}
