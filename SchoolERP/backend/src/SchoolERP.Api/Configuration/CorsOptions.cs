namespace SchoolERP.Api.Configuration;
public sealed class CorsOptions { public const string SectionName = "Cors"; public string[] AllowedOrigins { get; set; } = []; }