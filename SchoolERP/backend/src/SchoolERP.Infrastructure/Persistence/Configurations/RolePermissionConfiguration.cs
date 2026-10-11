using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolERP.Domain.Entities;
using SchoolERP.Infrastructure.Identity;

namespace SchoolERP.Infrastructure.Persistence.Configurations;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions"); builder.ConfigureAudit();
        builder.Property(x => x.RoleId).HasColumnName("role_id").IsRequired();
        builder.Property(x => x.PermissionId).HasColumnName("permission_id").IsRequired();
        builder.Property(x => x.IsSystemAssigned).HasColumnName("is_system_assigned").IsRequired();
        builder.HasOne<ApplicationRole>().WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Permission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique()
            .HasDatabaseName("ux_role_permissions_role_id_permission_id");
        builder.HasIndex(x => x.RoleId).HasDatabaseName("ix_role_permissions_role_id");
        builder.HasIndex(x => x.PermissionId).HasDatabaseName("ix_role_permissions_permission_id");
    }
}
