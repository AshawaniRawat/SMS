using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolERP.Infrastructure.Identity;

namespace SchoolERP.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> b)
    {
        b.ToTable("users", t =>
        {
            t.HasCheckConstraint("ck_users_scope", "(tenant_id IS NOT NULL OR (school_id IS NULL AND campus_id IS NULL)) AND (school_id IS NOT NULL OR campus_id IS NULL)");
            t.HasCheckConstraint("ck_users_status", "status BETWEEN 1 AND 5");
        });
        b.HasKey(x => x.Id).HasName("pk_users");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.UserName).HasColumnName("user_name").HasMaxLength(320);
        b.Property(x => x.NormalizedUserName).HasColumnName("normalized_user_name").HasMaxLength(320);
        b.Property(x => x.Email).HasColumnName("email").IsRequired().HasMaxLength(320);
        b.Property(x => x.NormalizedEmail).HasColumnName("normalized_email").IsRequired().HasMaxLength(320);
        b.Property(x => x.EmailConfirmed).HasColumnName("email_confirmed");
        b.Property(x => x.PasswordHash).HasColumnName("password_hash");
        b.Property(x => x.SecurityStamp).HasColumnName("security_stamp");
        b.Property(x => x.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
        b.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(30);
        b.Property(x => x.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
        b.Property(x => x.TwoFactorEnabled).HasColumnName("two_factor_enabled");
        b.Property(x => x.LockoutEnd).HasColumnName("lockout_end");
        b.Property(x => x.LockoutEnabled).HasColumnName("lockout_enabled");
        b.Property(x => x.AccessFailedCount).HasColumnName("access_failed_count");
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.SchoolId).HasColumnName("school_id");
        b.Property(x => x.CampusId).HasColumnName("campus_id");
        b.Property(x => x.FirstName).HasColumnName("first_name").IsRequired().HasMaxLength(100);
        b.Property(x => x.LastName).HasColumnName("last_name").IsRequired().HasMaxLength(100);
        b.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(200);
        b.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        b.Property(x => x.LastLoginAt).HasColumnName("last_login_at").HasColumnType("timestamp with time zone");
        b.Property(x => x.PasswordChangedAt).HasColumnName("password_changed_at").HasColumnType("timestamp with time zone");
        b.Property(x => x.LockedAt).HasColumnName("locked_at").HasColumnType("timestamp with time zone");
        b.Property(x => x.DeactivatedAt).HasColumnName("deactivated_at").HasColumnType("timestamp with time zone");
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
        b.Property(x => x.CreatedBy).HasColumnName("created_by");
        b.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        b.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Campus).WithMany().HasForeignKey(x => x.CampusId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.NormalizedUserName).IsUnique().HasDatabaseName("ux_users_normalized_user_name");
        b.HasIndex(x => x.NormalizedEmail).IsUnique().HasDatabaseName("ux_users_normalized_email");
        b.HasIndex(x => x.TenantId).HasDatabaseName("ix_users_tenant_id");
        b.HasIndex(x => new { x.TenantId, x.SchoolId }).HasDatabaseName("ix_users_tenant_id_school_id");
        b.HasIndex(x => new { x.TenantId, x.Status }).HasDatabaseName("ix_users_tenant_id_status");
        b.HasIndex(x => x.SchoolId).HasDatabaseName("ix_users_school_id");
        b.HasIndex(x => x.CampusId).HasDatabaseName("ix_users_campus_id");
    }
}
