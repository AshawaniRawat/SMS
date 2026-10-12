using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SchoolERP.Application.Abstractions;
using SchoolERP.Domain.Entities;
using SchoolERP.Infrastructure.Audit;
using SchoolERP.Infrastructure.Persistence.Context;
using SchoolERP.Infrastructure.Persistence.Interceptors;
using Xunit;
namespace SchoolERP.UnitTests;
public sealed class AuditLoggingTests
{
    [Fact] public void SanitizerRemovesSensitiveKeysRecursively()
    {
        var sanitizer=new AuditDataSanitizer(Options.Create(new AuditLogOptions()));
        var json=sanitizer.Serialize(new{displayName="Safe",password="never",nested=new{accessToken="never",value=7}})!;
        Assert.DoesNotContain("password",json,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("token",json,StringComparison.OrdinalIgnoreCase);Assert.Contains("Safe",json);Assert.Contains("value",json);
    }
    [Fact] public void SanitizerBoundsOversizedPayload()
    {
        var sanitizer=new AuditDataSanitizer(Options.Create(new AuditLogOptions{MaxMetadataBytes=1024}));
        var json=sanitizer.Serialize(new{value=new string('x',5000)})!;
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(json)<=1024);Assert.True(JsonDocument.Parse(json).RootElement.GetProperty("truncated").GetBoolean());
    }
    [Fact] public void AuditLogHasNoPublicMutationOrDeletionSurface()
    {
        Assert.All(typeof(AuditLog).GetProperties(BindingFlags.Public|BindingFlags.Instance),p=>Assert.False(p.SetMethod?.IsPublic??false));
        Assert.DoesNotContain(typeof(AuditLog).GetMethods(BindingFlags.Public|BindingFlags.Instance),m=>m.Name.StartsWith("Update")||m.Name.StartsWith("Delete"));
    }
    [Fact] public void SaveInterceptorCreatesEnrichedAuditWithoutRecursion()
    {
        var current=new Current();var http=new DefaultHttpContext{TraceIdentifier="request-1"};http.Items["SchoolERP.CorrelationId"]="correlation-1";
        var sanitizer=new AuditDataSanitizer(Options.Create(new AuditLogOptions()));
        var interceptor=new AuditSaveChangesInterceptor(current,current,new Client(),sanitizer,new HttpContextAccessor{HttpContext=http},TimeProvider.System);
        var dbOptions=new DbContextOptionsBuilder<SchoolErpDbContext>().UseNpgsql("Host=localhost;Database=unused;Username=unused").AddInterceptors(interceptor,new Suppress()).Options;
        using var db=new SchoolErpDbContext(dbOptions,current,new NoPlatform());db.Tenants.Add(Tenant.Create("Audit tenant","audit-tenant"));db.SaveChanges();
        var row=Assert.Single(db.AuditLogs.Local);Assert.Equal("Tenant.Created",row.Action);Assert.Equal(current.UserId,row.ActorUserId);Assert.Equal("correlation-1",row.CorrelationId);Assert.Equal("request-1",row.RequestId);Assert.Equal(DateTimeKind.Utc,row.OccurredAtUtc.Kind);
    }
    private sealed class Current:ICurrentUser,ICurrentTenant{public Guid? UserId{get;}=Guid.NewGuid();public Guid? TenantId=>null;public Guid? SchoolId=>null;public Guid? CampusId=>null;public string? Email=>"auditor@example.test";public bool IsAuthenticated=>true;public IReadOnlyCollection<string> Roles=>[];public bool IsAvailable=>false;}
    private sealed class Client:IClientRequestContext{public string? IpAddress=>"127.0.0.1";public string? UserAgent=>"audit-test";}
    private sealed class NoPlatform:IPlatformDataAccess{public bool IsEnabled=>false;}
    private sealed class Suppress:SaveChangesInterceptor{public override InterceptionResult<int> SavingChanges(DbContextEventData d,InterceptionResult<int> r)=>InterceptionResult<int>.SuppressWithResult(0);}
}
