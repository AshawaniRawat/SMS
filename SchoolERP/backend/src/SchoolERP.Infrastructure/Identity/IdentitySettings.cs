namespace SchoolERP.Infrastructure.Identity;

public sealed class IdentitySettings
{
    public const string SectionName = "Identity";
    public bool RequireConfirmedEmail { get; set; }
    public int PasswordRequiredLength { get; set; } = 8;
    public int PasswordRequiredUniqueChars { get; set; } = 4;
    public int MaxFailedAccessAttempts { get; set; } = 5;
    public int DefaultLockoutMinutes { get; set; } = 15;
}
