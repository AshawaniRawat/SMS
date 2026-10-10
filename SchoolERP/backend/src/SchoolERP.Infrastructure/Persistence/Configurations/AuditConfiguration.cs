using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Common;

namespace SchoolERP.Infrastructure.Persistence.Configurations;

internal static class AuditConfiguration
{
    public static void ConfigureAudit<TEntity>(this EntityTypeBuilder<TEntity> builder) where TEntity : AuditableEntity
    {
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(entity => entity.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.CreatedBy).HasColumnName("created_by");
        builder.Property(entity => entity.UpdatedBy).HasColumnName("updated_by");
        builder.Ignore(entity => entity.DomainEvents);
    }

    public static void ConfigureSoftDelete<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : AuditableEntity, SchoolERP.Domain.Interfaces.ISoftDeletableEntity
    {
        builder.Property(entity => entity.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(entity => entity.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone");
        builder.Property(entity => entity.DeletedBy).HasColumnName("deleted_by");
    }
}
