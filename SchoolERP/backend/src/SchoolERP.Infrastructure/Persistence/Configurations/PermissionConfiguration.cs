using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolERP.Domain.Entities;

namespace SchoolERP.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions", table =>
        {
            table.HasCheckConstraint("ck_permissions_code_not_blank", "length(btrim(code)) > 0");
            table.HasCheckConstraint("ck_permissions_name_not_blank", "length(btrim(name)) > 0");
            table.HasCheckConstraint("ck_permissions_module_not_blank", "length(btrim(module)) > 0");
            table.HasCheckConstraint("ck_permissions_action_not_blank", "length(btrim(action)) > 0");
            table.HasCheckConstraint("ck_permissions_scope", "scope BETWEEN 1 AND 5");
            table.HasCheckConstraint("ck_permissions_sort_order", "sort_order >= 0");
        });
        builder.ConfigureAudit();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Module).HasColumnName("module").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(x => x.Scope).HasColumnName("scope").HasConversion<int>().IsRequired();
        builder.Property(x => x.IsSystemPermission).HasColumnName("is_system_permission").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();
        builder.Property(x => x.Group).HasColumnName("group").HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_permissions_code");
        builder.HasIndex(x => x.Module).HasDatabaseName("ix_permissions_module");
        builder.HasIndex(x => x.Scope).HasDatabaseName("ix_permissions_scope");
        builder.HasIndex(x => x.IsActive).HasDatabaseName("ix_permissions_is_active");
        builder.HasIndex(x => new { x.Module, x.IsActive }).HasDatabaseName("ix_permissions_module_is_active");
    }
}
