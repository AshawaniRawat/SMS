using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolERP.Infrastructure.Identity;
namespace SchoolERP.Infrastructure.Persistence.Configurations;
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens"); b.ConfigureAudit();
        b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        b.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
        b.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc").HasColumnType("timestamp with time zone");
        b.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc").HasColumnType("timestamp with time zone");
        b.Property(x => x.RevokedReason).HasColumnName("revoked_reason").HasMaxLength(200);
        b.Property(x => x.ReplacedByTokenId).HasColumnName("replaced_by_token_id");
        b.Property(x => x.ParentTokenId).HasColumnName("parent_token_id");
        b.Property(x => x.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(64);
        b.Property(x => x.RevokedByIp).HasColumnName("revoked_by_ip").HasMaxLength(64);
        b.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        b.Property(x => x.IsUsed).HasColumnName("is_used");
        b.Property(x => x.UsedAtUtc).HasColumnName("used_at_utc").HasColumnType("timestamp with time zone");
        b.Property(x => x.TenantId).HasColumnName("tenant_id"); b.Property(x => x.SchoolId).HasColumnName("school_id");
        b.Property(x => x.CampusId).HasColumnName("campus_id"); b.Property(x => x.SessionId).HasColumnName("session_id").HasMaxLength(100).IsRequired();
        b.Property(x => x.SecurityStampHash).HasColumnName("security_stamp_hash").HasMaxLength(64).IsRequired();
        b.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        b.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ParentToken).WithMany().HasForeignKey(x => x.ParentTokenId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReplacedByToken).WithMany().HasForeignKey(x => x.ReplacedByTokenId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("ux_refresh_tokens_token_hash");
        b.HasIndex(x => x.UserId).HasDatabaseName("ix_refresh_tokens_user_id");
        b.HasIndex(x => new { x.UserId, x.SessionId }).HasDatabaseName("ix_refresh_tokens_user_id_session_id");
        b.HasIndex(x => x.ExpiresAtUtc).HasDatabaseName("ix_refresh_tokens_expires_at_utc");
    }
}
