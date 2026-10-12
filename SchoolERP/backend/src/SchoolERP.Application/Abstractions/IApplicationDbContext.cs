using Microsoft.EntityFrameworkCore;
using SchoolERP.Domain.Entities;

namespace SchoolERP.Application.Abstractions;

public interface IApplicationDbContext : IUnitOfWork
{
    DbSet<Tenant> Tenants { get; }
    DbSet<School> Schools { get; }
    DbSet<Campus> Campuses { get; }
    DbSet<AcademicYear> AcademicYears { get; }
    DbSet<SchoolClass> SchoolClasses { get; }
    DbSet<ClassSection> ClassSections { get; }
    DbSet<Subject> Subjects { get; }
    DbSet<ClassSubject> ClassSubjects { get; }
    DbSet<Department> Departments { get; }
    DbSet<House> Houses { get; }
    DbSet<SchoolCalendarEvent> SchoolCalendarEvents { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<AuditLog> AuditLogs { get; }
}
