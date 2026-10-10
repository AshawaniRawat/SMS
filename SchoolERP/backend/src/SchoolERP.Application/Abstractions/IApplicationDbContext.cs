using Microsoft.EntityFrameworkCore;
using SchoolERP.Domain.Entities;

namespace SchoolERP.Application.Abstractions;

public interface IApplicationDbContext : IUnitOfWork
{
    DbSet<Tenant> Tenants { get; }
    DbSet<School> Schools { get; }
    DbSet<Campus> Campuses { get; }
    DbSet<AcademicYear> AcademicYears { get; }
}
