using Microsoft.EntityFrameworkCore;
using SchoolERP.Application.Exceptions;
using SchoolERP.Domain.Entities;
using Xunit;
namespace SchoolERP.IntegrationTests;
[Collection(PostgresCollection.Name)]
public sealed class HousePersistenceTests(PostgresFixture database)
{
 [PostgresFact] public async Task Tenant_filter_and_school_wide_code_uniqueness_are_enforced()
 {
  var a=Tenant.Create("House Tenant A","house-a-"+Guid.NewGuid().ToString("N"));var b=Tenant.Create("House Tenant B","house-b-"+Guid.NewGuid().ToString("N"));var sa=School.Create(a,"School A","A");var sb=School.Create(b,"School B","B");var ha=House.Create(a.Id,sa.Id,"Red A","RED",null,"#FF0000",0);var hb=House.Create(b.Id,sb.Id,"Red B","RED",null,"#EE0000",0);
  await using(var setup=database.CreateContext(platformAccess:true)){setup.Schools.AddRange(sa,sb);setup.Houses.AddRange(ha,hb);await setup.SaveChangesAsync();}
  await using(var scoped=database.CreateContext(a.Id)){Assert.Equal(ha.Id,(await scoped.Houses.SingleAsync()).Id);}
  await using(var duplicate=database.CreateContext(a.Id)){duplicate.Houses.Add(House.Create(a.Id,sa.Id,"Crimson","RED",null,"#CC0000",1));await Assert.ThrowsAsync<HouseCodeConflictException>(()=>duplicate.SaveChangesAsync());}
 }
}
