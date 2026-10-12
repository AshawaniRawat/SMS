namespace SchoolERP.Infrastructure.Audit;
public sealed class AuditLogOptions
{
    public const string SectionName="AuditLog";
    public int DefaultQueryDays { get; set; }=30;
    public int MaxQueryRangeDays { get; set; }=365;
    public int? RetentionDays { get; set; }
    public int MaxMetadataBytes { get; set; }=16384;
}
