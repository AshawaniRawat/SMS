using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Features.Audit;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using SchoolERP.Domain.Security;
using SchoolERP.Infrastructure.Audit;

namespace SchoolERP.Infrastructure.Persistence.Interceptors;

public sealed class AuditSaveChangesInterceptor(ICurrentUser user, ICurrentTenant tenant, IClientRequestContext client,
    IAuditDataSanitizer sanitizer, IHttpContextAccessor accessor, TimeProvider clock) : SaveChangesInterceptor
{
    private static readonly HashSet<string> SnapshotFields=new(StringComparer.OrdinalIgnoreCase)
    { "Name","Code","ShortName","Status","IsActive","IsMainCampus","IsCurrent","IsAdmissionOpen","EmailConfirmed","Type","StartDate","EndDate","SchoolId","CampusId","TenantId","RoleId","PermissionId","ClassId","SubjectId" };

    public override InterceptionResult<int> SavingChanges(DbContextEventData data,InterceptionResult<int> result){AddEntries(data.Context);return result;}
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data,InterceptionResult<int> result,CancellationToken ct=default){AddEntries(data.Context);return ValueTask.FromResult(result);}

    private void AddEntries(DbContext? db)
    {
        if(db is null)return;
        var changed=db.ChangeTracker.Entries().Where(x=>x.Entity is not AuditLog&&x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
        foreach(var entry in changed)
        {
            var classified=Classify(entry);if(classified is null)continue;
            var http=accessor.HttpContext;var isLogin=entry.Metadata.ClrType.Name=="ApplicationUser"&&entry.State==EntityState.Modified&&entry.Property("LastLoginAt").IsModified;var actor=user.UserId??(isLogin?GuidValue(entry,"Id"):null);var tenantId=GuidValue(entry,"TenantId")??(tenant.IsAvailable?tenant.TenantId:user.TenantId);
            db.Set<AuditLog>().Add(AuditLog.Create(Guid.NewGuid(),tenantId,GuidValue(entry,"SchoolId")??user.SchoolId,Campus(entry)??user.CampusId,actor,null,AuditService.Mask(user.Email??StringValue(entry,"Email")),actor.HasValue?(tenantId.HasValue?AuditActorType.User:AuditActorType.PlatformUser):AuditActorType.System,
                classified.Value.Action,classified.Value.Category,classified.Value.EntityType,Id(entry),Display(entry),AuditOutcome.Succeeded,$"{classified.Value.Action} completed.",Snapshot(entry,true),Snapshot(entry,false),null,
                http?.Items["SchoolERP.CorrelationId"]?.ToString()??http?.Response.Headers["X-Correlation-ID"].ToString(),http?.TraceIdentifier,Activity.Current?.TraceId.ToString(),http?.User.FindFirstValue("session_id"),client.IpAddress,AuditService.Limit(client.UserAgent,512),clock.GetUtcNow().UtcDateTime));
        }
    }
    private string? Snapshot(EntityEntry e,bool before)
    {
        if(e.State==EntityState.Added&&before||e.State==EntityState.Deleted&&!before)return null;
        var values=before?e.OriginalValues:e.CurrentValues;var data=new Dictionary<string,object?>();
        foreach(var p in e.Properties.Where(x=>SnapshotFields.Contains(x.Metadata.Name)&&(e.State!=EntityState.Modified||x.IsModified)))data[p.Metadata.Name]=values[p.Metadata];
        return data.Count==0?null:sanitizer.Serialize(data);
    }
    private static (string Action,string Category,string EntityType)? Classify(EntityEntry e)
    {
        var type=e.Metadata.ClrType.Name;var added=e.State==EntityState.Added;var deleted=e.State==EntityState.Deleted;
        return type switch
        {
            nameof(Tenant)=>(added?AuditActions.Tenant.Created:e.Property("Status").IsModified?AuditActions.Tenant.StatusChanged:AuditActions.Tenant.Updated,"Tenant","Tenant"),
            nameof(School)=>(added?AuditActions.School.Created:e.Property("Status").IsModified?AuditActions.School.StatusChanged:AuditActions.School.ProfileUpdated,"School","School"),
            nameof(Campus)=>(added?AuditActions.Campus.Created:e.Property("IsMainCampus").IsModified&&Equals(e.CurrentValues["IsMainCampus"],true)?AuditActions.Campus.SetMain:e.Property("Status").IsModified?AuditActions.Campus.StatusChanged:AuditActions.Campus.Updated,"Campus","Campus"),
            nameof(AcademicYear)=>(added?AuditActions.AcademicYear.Created:e.Property("IsCurrent").IsModified&&Equals(e.CurrentValues["IsCurrent"],true)?AuditActions.AcademicYear.SetCurrent:e.Property("Status").IsModified?AuditActions.AcademicYear.StatusChanged:AuditActions.AcademicYear.Updated,"Academic","AcademicYear"),
            nameof(RolePermission)=>(deleted?AuditActions.Role.PermissionRemoved:AuditActions.Role.PermissionAssigned,"Security","RolePermission"),
            "ApplicationUser"=>(added?AuditActions.Identity.UserCreated:e.Property("LastLoginAt").IsModified?AuditActions.Authentication.LoginSucceeded:AuditActions.Identity.UserUpdated, e.Property("LastLoginAt").IsModified?"Authentication":"Identity","ApplicationUser"),
            "ApplicationRole"=>(added?AuditActions.Identity.RoleCreated:AuditActions.Identity.RoleUpdated,"Identity","ApplicationRole"),
            nameof(SchoolClass)=>(AuditActions.Academic.ClassChanged,"Academic","SchoolClass"), nameof(ClassSection)=>(AuditActions.Academic.SectionChanged,"Academic","ClassSection"), nameof(Subject)=>(AuditActions.Academic.SubjectChanged,"Academic","Subject"), nameof(ClassSubject)=>(AuditActions.Academic.SubjectAssignedToClass,"Academic","ClassSubject"), nameof(Department)=>(AuditActions.Academic.DepartmentChanged,"Academic","Department"), nameof(House)=>(AuditActions.Academic.HouseChanged,"Academic","House"), nameof(SchoolCalendarEvent)=>(AuditActions.Academic.CalendarEventChanged,"Academic","SchoolCalendarEvent"),
            _=>null
        };
    }
    private static Guid? GuidValue(EntityEntry e,string name){var p=e.Properties.FirstOrDefault(x=>x.Metadata.Name==name);return p?.CurrentValue is Guid g?g:p?.CurrentValue as Guid?;}
    private static string? StringValue(EntityEntry e,string name)=>e.Properties.FirstOrDefault(x=>x.Metadata.Name==name)?.CurrentValue?.ToString();
    private static Guid? Campus(EntityEntry e)=>e.Metadata.ClrType.Name==nameof(Campus)?GuidValue(e,"Id"):GuidValue(e,"CampusId");
    private static string? Id(EntityEntry e)=>e.Properties.FirstOrDefault(x=>x.Metadata.IsPrimaryKey())?.CurrentValue?.ToString();
    private static string? Display(EntityEntry e)=>e.Properties.FirstOrDefault(x=>x.Metadata.Name=="Name")?.CurrentValue?.ToString()??e.Properties.FirstOrDefault(x=>x.Metadata.Name=="Code")?.CurrentValue?.ToString();
}
