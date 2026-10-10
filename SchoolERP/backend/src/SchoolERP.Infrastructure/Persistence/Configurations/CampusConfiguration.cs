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
            table.HasCheckConstraint("ck_campuses_status", "status BETWEEN 1 AND 4");
            table.HasCheckConstraint("ck_campuses_latitude", "latitude IS NULL OR latitude BETWEEN -90 AND 90");
            table.HasCheckConstraint("ck_campuses_longitude", "longitude IS NULL OR longitude BETWEEN -180 AND 180");
        });
        builder.ConfigureAudit(); builder.ConfigureSoftDelete();
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(x => x.Code).HasColumnName("code").IsRequired().HasMaxLength(50);
        builder.Property(x => x.ShortName).HasColumnName("short_name").HasMaxLength(100);
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(320);
        builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(30);
        builder.Property(x => x.AlternatePhone).HasColumnName("alternate_phone").HasMaxLength(30);
        builder.Property(x => x.AddressLine1).HasColumnName("address_line1").HasMaxLength(250);
        builder.Property(x => x.AddressLine2).HasColumnName("address_line2").HasMaxLength(250);
        builder.Property(x => x.City).HasColumnName("city").HasMaxLength(100);
        builder.Property(x => x.State).HasColumnName("state").HasMaxLength(100);
        builder.Property(x => x.PostalCode).HasColumnName("postal_code").HasMaxLength(20);
        builder.Property(x => x.CountryCode).HasColumnName("country_code").HasMaxLength(2);
        builder.Property(x => x.TimeZoneId).HasColumnName("time_zone_id").HasMaxLength(100);
        builder.Property(x => x.Latitude).HasColumnName("latitude").HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasColumnName("longitude").HasPrecision(9, 6);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(x => x.IsMainCampus).HasColumnName("is_main_campus").IsRequired();
        builder.Property(x => x.ActivatedAt).HasColumnName("activated_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.SuspendedAt).HasColumnName("suspended_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ArchivedAt).HasColumnName("archived_at").HasColumnType("timestamp with time zone");
        builder.HasOne(x => x.Tenant).WithMany(x => x.Campuses).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.School).WithMany(x => x.Campuses).HasForeignKey(x => new { x.TenantId, x.SchoolId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_campuses_tenant_id");
        builder.HasIndex(x => x.SchoolId).HasDatabaseName("ix_campuses_school_id");
        builder.HasIndex(x => new { x.TenantId, x.SchoolId }).HasDatabaseName("ix_campuses_tenant_id_school_id");
        builder.HasIndex(x => new { x.SchoolId, x.Code }).IsUnique().HasDatabaseName("ux_campuses_school_id_code");
        builder.HasIndex(x => x.SchoolId).IsUnique().HasFilter("is_main_campus = true AND is_deleted = false")
            .HasDatabaseName("ux_campuses_school_id_main");
    }
}
