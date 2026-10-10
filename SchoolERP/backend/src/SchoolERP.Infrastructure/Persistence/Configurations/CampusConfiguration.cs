using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolERP.Domain.Entities;

namespace SchoolERP.Infrastructure.Persistence.Configurations;

public sealed class CampusConfiguration : IEntityTypeConfiguration<Campus>
{
    public void Configure(EntityTypeBuilder<Campus> builder)
    {
        builder.ToTable("campuses", table =>
        {
            table.HasCheckConstraint("ck_campuses_name_not_blank", "length(btrim(name)) > 0");
            table.HasCheckConstraint("ck_campuses_code_not_blank", "length(btrim(code)) > 0");
        });
        builder.ConfigureAudit();
        builder.ConfigureSoftDelete();
        builder.Property(entity => entity.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(entity => entity.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(entity => entity.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(entity => entity.Code).HasColumnName("code").IsRequired().HasMaxLength(50);
        builder.Property(entity => entity.Address).HasColumnName("address").HasMaxLength(1000);
        builder.Property(entity => entity.IsActive).HasColumnName("is_active").IsRequired();
        builder.HasOne(entity => entity.Tenant).WithMany(tenant => tenant.Campuses)
            .HasForeignKey(entity => entity.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.School).WithMany(school => school.Campuses)
            .HasForeignKey(entity => new { entity.TenantId, entity.SchoolId })
            .HasPrincipalKey(school => new { school.TenantId, school.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.TenantId).HasDatabaseName("ix_campuses_tenant_id");
        builder.HasIndex(entity => entity.SchoolId).HasDatabaseName("ix_campuses_school_id");
        builder.HasIndex(entity => new { entity.SchoolId, entity.Code }).IsUnique().HasDatabaseName("ux_campuses_school_id_code");
    }
}
