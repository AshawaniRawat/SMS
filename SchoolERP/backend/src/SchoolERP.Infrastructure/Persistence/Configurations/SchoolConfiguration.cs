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
            table.HasCheckConstraint("ck_schools_status", "status BETWEEN 1 AND 4");
        });
        builder.ConfigureAudit();
        builder.ConfigureSoftDelete();
        builder.Property(entity => entity.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(entity => entity.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(entity => entity.Code).HasColumnName("code").IsRequired().HasMaxLength(50);
        builder.Property(entity => entity.ShortName).HasColumnName("short_name").HasMaxLength(100);
        builder.Property(entity => entity.RegistrationNumber).HasColumnName("registration_number").HasMaxLength(100);
        builder.Property(entity => entity.AffiliationNumber).HasColumnName("affiliation_number").HasMaxLength(100);
        builder.Property(entity => entity.BoardCode).HasColumnName("board_code").HasMaxLength(100);
        builder.Property(entity => entity.BoardType).HasColumnName("board_type").HasConversion<int>();
        builder.Property(entity => entity.Email).HasColumnName("email").HasMaxLength(320);
        builder.Property(entity => entity.Phone).HasColumnName("phone").HasMaxLength(30);
        builder.Property(entity => entity.AlternatePhone).HasColumnName("alternate_phone").HasMaxLength(30);
        builder.Property(entity => entity.Website).HasColumnName("website").HasMaxLength(500);
        builder.Property(entity => entity.AddressLine1).HasColumnName("address_line1").HasMaxLength(250);
        builder.Property(entity => entity.AddressLine2).HasColumnName("address_line2").HasMaxLength(250);
        builder.Property(entity => entity.City).HasColumnName("city").HasMaxLength(100);
        builder.Property(entity => entity.State).HasColumnName("state").HasMaxLength(100);
        builder.Property(entity => entity.PostalCode).HasColumnName("postal_code").HasMaxLength(20);
        builder.Property(entity => entity.CountryCode).HasColumnName("country_code").HasMaxLength(2);
        builder.Property(entity => entity.LogoUrl).HasColumnName("logo_url").HasMaxLength(1000);
        builder.Property(entity => entity.TimeZoneId).HasColumnName("time_zone_id").HasMaxLength(100);
        builder.Property(entity => entity.DefaultCurrency).HasColumnName("default_currency").HasMaxLength(3);
        builder.Property(entity => entity.PrincipalName).HasColumnName("principal_name").HasMaxLength(200);
        builder.Property(entity => entity.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(entity => entity.ActivatedAt).HasColumnName("activated_at").HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.SuspendedAt).HasColumnName("suspended_at").HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.ArchivedAt).HasColumnName("archived_at").HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.Version).HasColumnName("version").IsConcurrencyToken().IsRequired();
        builder.HasOne(entity => entity.Tenant).WithMany(tenant => tenant.Schools)
            .HasForeignKey(entity => entity.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.TenantId).HasDatabaseName("ix_schools_tenant_id");
        builder.HasIndex(entity => new { entity.TenantId, entity.Status }).HasDatabaseName("ix_schools_tenant_id_status");
        builder.HasIndex(entity => new { entity.TenantId, entity.Code }).IsUnique().HasDatabaseName("ux_schools_tenant_id_code");
        // Allows the campus FK to enforce that its SchoolId and TenantId agree.
        builder.HasAlternateKey(entity => new { entity.TenantId, entity.Id }).HasName("ak_schools_tenant_id_id");
    }
}
