using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolERP.Domain.Entities;

namespace SchoolERP.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants", table =>
        {
            table.HasCheckConstraint("ck_tenants_slug_format", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
            table.HasCheckConstraint("ck_tenants_name_not_blank", "length(btrim(name)) > 0");
            table.HasCheckConstraint("ck_tenants_status", "status BETWEEN 1 AND 4");
        });
        builder.ConfigureAudit();
        builder.Property(entity => entity.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(entity => entity.Slug).HasColumnName("slug").IsRequired().HasMaxLength(100);
        builder.Property(entity => entity.LegalName).HasColumnName("legal_name").HasMaxLength(250);
        builder.Property(entity => entity.Email).HasColumnName("email").HasMaxLength(320);
        builder.Property(entity => entity.Phone).HasColumnName("phone").HasMaxLength(30);
        builder.Property(entity => entity.Website).HasColumnName("website").HasMaxLength(500);
        builder.Property(entity => entity.LogoUrl).HasColumnName("logo_url").HasMaxLength(1000);
        builder.Property(entity => entity.TimeZoneId).HasColumnName("time_zone_id").HasMaxLength(100);
        builder.Property(entity => entity.DefaultCurrency).HasColumnName("default_currency").HasMaxLength(3);
        builder.Property(entity => entity.CountryCode).HasColumnName("country_code").HasMaxLength(2);
        builder.Property(entity => entity.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(entity => entity.ActivatedAt).HasColumnName("activated_at").HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.SuspendedAt).HasColumnName("suspended_at").HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.ArchivedAt).HasColumnName("archived_at").HasColumnType("timestamp with time zone");
        builder.HasIndex(entity => entity.Slug).IsUnique().HasDatabaseName("ux_tenants_slug");
    }
}
