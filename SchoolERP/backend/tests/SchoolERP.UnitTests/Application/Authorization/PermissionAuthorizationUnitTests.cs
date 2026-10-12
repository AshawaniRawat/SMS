using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SchoolERP.Api.Security;
using SchoolERP.Application.Security;
using SchoolERP.UnitTests.TestCommon;
using SchoolERP.UnitTests.TestCommon.Fakes;
using Xunit;
namespace SchoolERP.UnitTests.Application.Authorization;
public sealed class PermissionAuthorizationUnitTests
{
    [Fact] public void PermissionRequirement_WhenConstructed_PreservesOrdinalCode(){var requirement=new PermissionRequirement("Campus.View");Assert.Equal("Campus.View",requirement.PermissionCode);Assert.NotEqual("campus.view",requirement.PermissionCode);}
    [Fact] public void RequirePermissionAttribute_WhenConstructed_SetsPermissionPolicy()=>Assert.Equal("Permission:Campus.View",new RequirePermissionAttribute("Campus.View").Policy);
    [Fact] public async Task PolicyProvider_ForPermissionName_CreatesAuthenticatedPermissionPolicy(){var provider=new PermissionAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions()));var policy=await provider.GetPolicyAsync("Permission:Campus.View");Assert.NotNull(policy);Assert.Contains(policy.Requirements,x=>x is DenyAnonymousAuthorizationRequirement);Assert.Contains(policy.Requirements,x=>x is PermissionRequirement r&&r.PermissionCode=="Campus.View");}
    [Fact] public async Task PolicyProvider_ForEmptyPermission_ReturnsNull(){var provider=new PermissionAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions()));Assert.Null(await provider.GetPolicyAsync("Permission:"));}
    [Fact] public async Task Handler_WhenPermissionGranted_Succeeds()
    {
        var permissions=Substitute.For<IUserPermissionService>();permissions.HasPermissionAsync(TestIds.UserA,"Campus.View",Arg.Any<CancellationToken>()).Returns(true);var user=new FakeCurrentUser();var http=new HttpContextAccessor{HttpContext=new DefaultHttpContext()};var handler=new PermissionAuthorizationHandler(permissions,user,http,NullLogger<PermissionAuthorizationHandler>.Instance);var requirement=new PermissionRequirement("Campus.View");var context=new AuthorizationHandlerContext([requirement],new ClaimsPrincipal(),null);await handler.HandleAsync(context);Assert.True(context.HasSucceeded);
    }
    [Fact] public async Task Handler_WhenPermissionMissing_DoesNotSucceed()
    {
        var permissions=Substitute.For<IUserPermissionService>();permissions.HasPermissionAsync(Arg.Any<Guid>(),Arg.Any<string>(),Arg.Any<CancellationToken>()).Returns(false);var handler=new PermissionAuthorizationHandler(permissions,new FakeCurrentUser(),new HttpContextAccessor{HttpContext=new DefaultHttpContext()},NullLogger<PermissionAuthorizationHandler>.Instance);var context=new AuthorizationHandlerContext([new PermissionRequirement("Campus.View")],new ClaimsPrincipal(),null);await handler.HandleAsync(context);Assert.False(context.HasSucceeded);
    }
    [Fact] public async Task Handler_WhenPlatformUserLacksPermission_DoesNotAutoSucceed(){var permissions=Substitute.For<IUserPermissionService>();var user=new FakeCurrentUser{TenantId=null,SchoolId=null,CampusId=null,IsPlatformUser=true};var handler=new PermissionAuthorizationHandler(permissions,user,new HttpContextAccessor{HttpContext=new DefaultHttpContext()},NullLogger<PermissionAuthorizationHandler>.Instance);var context=new AuthorizationHandlerContext([new PermissionRequirement("Audit.PlatformView")],new ClaimsPrincipal(),null);await handler.HandleAsync(context);Assert.False(context.HasSucceeded);}
}
