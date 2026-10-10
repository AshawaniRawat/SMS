using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolERP.Infrastructure.Identity;

namespace SchoolERP.Infrastructure.Persistence.Configurations;

public sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> b)
    {
        b.ToTable("roles"); b.HasKey(x => x.Id).HasName("pk_roles");
        b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.Name).HasColumnName("name").HasMaxLength(256);
        b.Property(x => x.NormalizedName).HasColumnName("normalized_name").HasMaxLength(256);
        b.Property(x => x.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        b.Property(x => x.IsSystemRole).HasColumnName("is_system_role");
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
        b.Property(x => x.CreatedBy).HasColumnName("created_by"); b.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        b.HasIndex(x => x.NormalizedName).IsUnique().HasDatabaseName("ux_roles_normalized_name");
    }
}
