using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    [Required] public string ConnectionStringName { get; set; } = "DefaultConnection";
    public bool ApplyMigrationsOnStartup { get; set; }
    public bool SeedDevelopmentData { get; set; }
    [Range(0, 5)] public int MaxRetryCount { get; set; } = 3;
    [Range(1, 30)] public int MaxRetryDelaySeconds { get; set; } = 5;
    [Range(1, 300)] public int CommandTimeoutSeconds { get; set; } = 30;
    public bool EnableSensitiveDataLogging { get; set; }
    public bool EnableDetailedErrors { get; set; }
}
