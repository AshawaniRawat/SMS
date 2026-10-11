using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolERP.Application.Exceptions;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using Xunit;
namespace SchoolERP.IntegrationTests;
[Collection(PostgresCollection.Name)]
public sealed class DepartmentPersistenceTests(PostgresFixture database)
{
 [PostgresFact] public async Task Tenant_filter_and_campus_hierarchy_are_enforced(){var a=Tenant.Create("Department A","dept-a-"+Guid.NewGuid().ToString("N"));var b=Tenant.Create("Department B","dept-b-"+Guid.NewGuid().ToString("N"));var sa=School.Create(a,"School A","SA");var sb=School.Create(b,"School B","SB");var campus=Campus.Create(b.Id,sb.Id,"B Campus","BC");await using(var setup=database.CreateContext(platformAccess:true)){setup.Schools.AddRange(sa,sb);setup.Campuses.Add(campus);await setup.SaveChangesAsync();}await using(var invalid=database.CreateContext(platformAccess:true)){invalid.Departments.Add(Department.Create(a.Id,sa.Id,campus.Id,"Math","MATH",DepartmentType.Academic,0));var ex=await Assert.ThrowsAsync<DbUpdateException>(()=>invalid.SaveChangesAsync());Assert.Equal(PostgresErrorCodes.ForeignKeyViolation,Assert.IsType<PostgresException>(ex.InnerException).SqlState);}await using var scoped=database.CreateContext(a.Id);Assert.Empty(await scoped.Departments.ToListAsync());}
 [PostgresFact] public async Task Code_is_unique_across_campuses_in_one_school_but_allowed_in_another_school(){var t=Tenant.Create("Department Unique","dept-u-"+Guid.NewGuid().ToString("N"));var a=School.Create(t,"School A","A");var b=School.Create(t,"School B","B");var ca=Campus.Create(t.Id,a.Id,"A Campus","CA");var cb=Campus.Create(t.Id,a.Id,"B Campus","CB");await using var db=database.CreateContext(platformAccess:true);db.Schools.AddRange(a,b);db.Campuses.AddRange(ca,cb);db.Departments.AddRange(Department.Create(t.Id,a.Id,ca.Id,"Math A","MATH",DepartmentType.Academic,0),Department.Create(t.Id,b.Id,null,"Math B","MATH",DepartmentType.Academic,0));await db.SaveChangesAsync();db.Departments.Add(Department.Create(t.Id,a.Id,cb.Id,"Math C","MATH",DepartmentType.Academic,0));await Assert.ThrowsAsync<DepartmentCodeConflictException>(()=>db.SaveChangesAsync());}
}
