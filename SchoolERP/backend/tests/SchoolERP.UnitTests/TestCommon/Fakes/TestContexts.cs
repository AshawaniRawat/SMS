using SchoolERP.Application.Abstractions;
using SchoolERP.Api.Logging;
namespace SchoolERP.UnitTests.TestCommon.Fakes;
public sealed class FakeCurrentUser:ICurrentUser
{
    public Guid? UserId{get;set;}=TestIds.UserA;public Guid? TenantId{get;set;}=TestIds.TenantA;public Guid? SchoolId{get;set;}=TestIds.SchoolA;public Guid? CampusId{get;set;}=TestIds.CampusA;public string? Email{get;set;}=TestData.Email;public bool IsAuthenticated{get;set;}=true;public bool IsPlatformUser{get;set;}public IReadOnlyCollection<string> Roles{get;set;}=[];
}
public sealed class FakeCurrentTenant:ICurrentTenant{public Guid? TenantId{get;set;}=TestIds.TenantA;public bool IsAvailable{get;set;}=true;public bool IsPlatformScope{get;set;}}
public sealed class FakeCorrelationContext:ICorrelationContext{public string CorrelationId{get;set;}="test-correlation-id";public string RequestId{get;set;}="test-request-id";public string TraceId{get;set;}="test-trace-id";}
