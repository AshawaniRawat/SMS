namespace SchoolERP.Infrastructure.Persistence.Seed;

public sealed class DevelopmentBootstrapOptions
{
    public const string SectionName = "DevelopmentBootstrap";
    public bool Enabled { get; set; }
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
}
