using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolERP.Domain.Entities;

namespace SchoolERP.Infrastructure.Persistence.Configurations;

public sealed class SchoolConfiguration : IEntityTypeConfiguration<School>
{
    public void Configure(EntityTypeBuilder<School> builder)
    {
        builder.ToTable("schools", table =>
        {
            table.HasCheckConstraint("ck_schools_name_not_blank", "length(btrim(name)) > 0");
            table.HasCheckConstraint("ck_schools_code_not_blank", "length(btrim(code)) > 0");
        });
        builder.ConfigureAudit();
        builder.ConfigureSoftDelete();
        builder.Property(entity => entity.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(entity => entity.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(entity => entity.Code).HasColumnName("code").IsRequired().HasMaxLength(50);
        builder.Property(entity => entity.Email).HasColumnName("email").HasMaxLength(320);
        builder.Property(entity => entity.Phone).HasColumnName("phone").HasMaxLength(30);
        builder.Property(entity => entity.Address).HasColumnName("address").HasMaxLength(1000);
        builder.Property(entity => entity.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(entity => entity.Version).HasColumnName("version").IsConcurrencyToken().IsRequired();
        builder.HasOne(entity => entity.Tenant).WithMany(tenant => tenant.Schools)
            .HasForeignKey(entity => entity.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.TenantId).HasDatabaseName("ix_schools_tenant_id");
        builder.HasIndex(entity => new { entity.TenantId, entity.Code }).IsUnique().HasDatabaseName("ux_schools_tenant_id_code");
        // Allows the campus FK to enforce that its SchoolId and TenantId agree.
        builder.HasAlternateKey(entity => new { entity.TenantId, entity.Id }).HasName("ak_schools_tenant_id_id");
    }
}
