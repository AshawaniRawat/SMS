using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolERP.Domain.Entities;

namespace SchoolERP.Infrastructure.Persistence.Configurations;

public sealed class AcademicYearConfiguration : IEntityTypeConfiguration<AcademicYear>
{
    public void Configure(EntityTypeBuilder<AcademicYear> builder)
    {
        builder.ToTable("academic_years", table =>
        {
            table.HasCheckConstraint("ck_academic_years_name_not_blank", "length(btrim(name)) > 0");
            table.HasCheckConstraint("ck_academic_years_code_not_blank", "length(btrim(code)) > 0");
            table.HasCheckConstraint("ck_academic_years_dates", "end_date > start_date");
            table.HasCheckConstraint("ck_academic_years_admission_dates", "admission_start_date IS NULL OR admission_end_date IS NULL OR admission_end_date >= admission_start_date");
            table.HasCheckConstraint("ck_academic_years_lock_date", "lock_date IS NULL OR lock_date BETWEEN start_date AND end_date");
            table.HasCheckConstraint("ck_academic_years_status", "status BETWEEN 1 AND 4");
        });
        builder.ConfigureAudit();
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired(); builder.Property(x => x.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired(); builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.StartDate).HasColumnName("start_date").HasColumnType("date").IsRequired(); builder.Property(x => x.EndDate).HasColumnName("end_date").HasColumnType("date").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().IsRequired(); builder.Property(x => x.IsCurrent).HasColumnName("is_current").IsRequired();
        builder.Property(x => x.IsAdmissionOpen).HasColumnName("is_admission_open").IsRequired();
        builder.Property(x => x.AdmissionStartDate).HasColumnName("admission_start_date").HasColumnType("date"); builder.Property(x => x.AdmissionEndDate).HasColumnName("admission_end_date").HasColumnType("date");
        builder.Property(x => x.LockDate).HasColumnName("lock_date").HasColumnType("date"); builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000);
        builder.HasOne(x => x.Tenant).WithMany(x => x.AcademicYears).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.School).WithMany(x => x.AcademicYears).HasForeignKey(x => new { x.TenantId, x.SchoolId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_academic_years_tenant_id");
        builder.HasIndex(x => x.SchoolId).HasDatabaseName("ix_academic_years_school_id");
        builder.HasIndex(x => new { x.TenantId, x.SchoolId }).HasDatabaseName("ix_academic_years_tenant_id_school_id");
        builder.HasIndex(x => new { x.SchoolId, x.Code }).IsUnique().HasDatabaseName("ux_academic_years_school_id_code");
        builder.HasIndex(x => new { x.SchoolId, x.Status }).HasDatabaseName("ix_academic_years_school_id_status");
        builder.HasIndex(x => new { x.SchoolId, x.StartDate }).HasDatabaseName("ix_academic_years_school_id_start_date");
        builder.HasIndex(x => x.SchoolId).IsUnique().HasFilter("is_current = true").HasDatabaseName("ux_academic_years_school_id_current");
        builder.HasAlternateKey(x=>new{x.TenantId,x.SchoolId,x.Id}).HasName("ak_academic_years_tenant_school_id");
    }
}
