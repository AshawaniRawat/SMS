using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolERP.Infrastructure.Identity;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Entities;

namespace SchoolERP.Infrastructure.Persistence.Configurations;

public sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> b)
    {
        b.ToTable("roles"); b.HasKey(x => x.Id).HasName("pk_roles");
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever(); b.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        b.Property(x => x.NormalizedName).HasColumnName("normalized_name").HasMaxLength(150).IsRequired();
        b.Property(x => x.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
        b.Property(x => x.TenantId).HasColumnName("tenant_id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        b.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(150).IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        b.Property(x => x.Type).HasColumnName("type").HasConversion<int>();
        b.Property(x => x.IsSystemRole).HasColumnName("is_system_role");
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<int>();
        b.Property(x => x.SortOrder).HasColumnName("sort_order");
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
        b.Property(x => x.CreatedBy).HasColumnName("created_by"); b.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        b.HasIndex(x => x.NormalizedName).IsUnique().HasDatabaseName("ux_roles_normalized_name");
        b.HasIndex(x => x.TenantId).HasDatabaseName("ix_roles_tenant_id");
        b.HasIndex(x => x.Status).HasDatabaseName("ix_roles_status");
        b.HasIndex(x => x.Type).HasDatabaseName("ix_roles_type");
        b.HasIndex(x => x.IsSystemRole).HasDatabaseName("ix_roles_is_system_role");
        b.HasIndex(x => new { x.TenantId, x.Status }).HasDatabaseName("ix_roles_tenant_id_status");
        b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique().HasFilter("tenant_id IS NOT NULL")
            .HasDatabaseName("ux_roles_tenant_id_code");
        b.HasIndex(x => x.Code).IsUnique().HasFilter("tenant_id IS NULL").HasDatabaseName("ux_roles_global_code");
        b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_roles_status", "status BETWEEN 1 AND 3");
            t.HasCheckConstraint("ck_roles_type", "type BETWEEN 1 AND 4");
            t.HasCheckConstraint("ck_roles_scope", "(is_system_role AND tenant_id IS NULL) OR (NOT is_system_role AND tenant_id IS NOT NULL)");
        });
        b.HasData(CoreRoleCatalog.All.Select(x => ApplicationRole.CreateSystem(
            x.Id, x.Code, x.DisplayName, x.Type, null, x.SortOrder)));
    }
}
