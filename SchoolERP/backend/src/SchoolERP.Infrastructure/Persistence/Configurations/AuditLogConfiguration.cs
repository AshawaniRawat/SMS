using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolERP.Domain.Entities;
namespace SchoolERP.Infrastructure.Persistence.Configurations;
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs"); b.HasKey(x=>x.Id); b.Property(x=>x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x=>x.TenantId).HasColumnName("tenant_id"); b.Property(x=>x.SchoolId).HasColumnName("school_id"); b.Property(x=>x.CampusId).HasColumnName("campus_id");
        b.Property(x=>x.ActorUserId).HasColumnName("actor_user_id"); b.Property(x=>x.ActorDisplayName).HasColumnName("actor_display_name").HasMaxLength(200); b.Property(x=>x.ActorEmailMasked).HasColumnName("actor_email_masked").HasMaxLength(320);
        b.Property(x=>x.ActorType).HasColumnName("actor_type").HasConversion<string>().HasMaxLength(32); b.Property(x=>x.Action).HasColumnName("action").HasMaxLength(160).IsRequired(); b.Property(x=>x.Category).HasColumnName("category").HasMaxLength(64).IsRequired();
        b.Property(x=>x.EntityType).HasColumnName("entity_type").HasMaxLength(100); b.Property(x=>x.EntityId).HasColumnName("entity_id").HasMaxLength(200); b.Property(x=>x.EntityDisplayName).HasColumnName("entity_display_name").HasMaxLength(300);
        b.Property(x=>x.Outcome).HasColumnName("outcome").HasConversion<string>().HasMaxLength(24); b.Property(x=>x.Description).HasColumnName("description").HasMaxLength(500);
        b.Property(x=>x.BeforeJson).HasColumnName("before_json").HasColumnType("jsonb"); b.Property(x=>x.AfterJson).HasColumnName("after_json").HasColumnType("jsonb"); b.Property(x=>x.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
        b.Property(x=>x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128); b.Property(x=>x.RequestId).HasColumnName("request_id").HasMaxLength(128); b.Property(x=>x.TraceId).HasColumnName("trace_id").HasMaxLength(64); b.Property(x=>x.SessionId).HasColumnName("session_id").HasMaxLength(128);
        b.Property(x=>x.IpAddress).HasColumnName("ip_address").HasMaxLength(64); b.Property(x=>x.UserAgent).HasColumnName("user_agent").HasMaxLength(512); b.Property(x=>x.OccurredAtUtc).HasColumnName("occurred_at_utc").HasColumnType("timestamp with time zone");
        b.HasIndex(x=>new{x.TenantId,x.OccurredAtUtc}).HasDatabaseName("ix_audit_logs_tenant_occurred"); b.HasIndex(x=>new{x.SchoolId,x.OccurredAtUtc}).HasDatabaseName("ix_audit_logs_school_occurred"); b.HasIndex(x=>new{x.CampusId,x.OccurredAtUtc}).HasDatabaseName("ix_audit_logs_campus_occurred"); b.HasIndex(x=>new{x.Action,x.OccurredAtUtc}).HasDatabaseName("ix_audit_logs_action_occurred"); b.HasIndex(x=>new{x.EntityType,x.EntityId}).HasDatabaseName("ix_audit_logs_entity"); b.HasIndex(x=>x.CorrelationId).HasDatabaseName("ix_audit_logs_correlation");
    }
}
