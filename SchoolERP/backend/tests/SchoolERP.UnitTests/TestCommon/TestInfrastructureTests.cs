using SchoolERP.UnitTests.TestCommon.Builders;
using SchoolERP.UnitTests.TestCommon.Fakes;
using Xunit;
namespace SchoolERP.UnitTests.TestCommon;
public sealed class TestInfrastructureTests
{
    [Fact]public void TestClock_ByDefault_ReturnsFixedUtcInstant(){var clock=new TestClock();Assert.Equal(new DateTimeOffset(2026,1,15,10,0,0,TimeSpan.Zero),clock.GetUtcNow());clock.Advance(TimeSpan.FromHours(1));Assert.Equal(11,clock.GetUtcNow().Hour);}
    [Fact]public void FakeTenant_WhenTenantMissing_DoesNotImplyPlatformScope(){var tenant=new FakeCurrentTenant{TenantId=null,IsAvailable=false};Assert.False(tenant.IsPlatformScope);}
    [Fact]public void EntityBuilders_WithDefaults_CreateValidEntities()
    {
        Assert.NotNull(new TenantBuilder().Build());Assert.NotNull(new SchoolBuilder().Build());Assert.NotNull(new CampusBuilder().Build());Assert.NotNull(new AcademicYearBuilder().Build());Assert.NotNull(new SchoolClassBuilder().Build());Assert.NotNull(new ClassSectionBuilder().Build());Assert.NotNull(new SubjectBuilder().Build());Assert.NotNull(new DepartmentBuilder().Build());Assert.NotNull(new HouseBuilder().Build());Assert.NotNull(new CalendarEventBuilder().Build());Assert.NotNull(new UserBuilder().Build());Assert.NotNull(new RoleBuilder().Build());
    }
}
