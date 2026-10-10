namespace SchoolERP.Application.Security;
public sealed class RefreshTokenOptions
{
    public const string SectionName = "Authentication";
    public int RefreshTokenLifetimeDays { get; set; } = 7;
    public int RefreshTokenRetentionDays { get; set; } = 30;
}
