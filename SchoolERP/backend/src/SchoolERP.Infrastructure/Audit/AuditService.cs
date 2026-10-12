using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SchoolERP.Application.Abstractions;
using SchoolERP.Application.Common.Pagination;
using SchoolERP.Application.Features.Audit;
using SchoolERP.Domain.Entities;
using SchoolERP.Domain.Enums;
using SchoolERP.Infrastructure.Persistence.Context;
namespace SchoolERP.Infrastructure.Audit;

public sealed class AuditService(SchoolErpDbContext db, ICurrentUser user, ICurrentTenant tenant,
    IClientRequestContext client, IAuditDataSanitizer sanitizer, IHttpContextAccessor accessor,
    TimeProvider clock) : IAuditService
{
    public async Task WriteAsync(AuditEntry e,CancellationToken ct=default)
    {
        var http=accessor.HttpContext; var actor=e.ActorUserId??user.UserId;
        db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(),e.TenantId??(tenant.IsAvailable?tenant.TenantId:user.TenantId),e.SchoolId??user.SchoolId,e.CampusId??user.CampusId,actor,
            null,Mask(user.Email),e.ActorType??(actor.HasValue?(user.TenantId.HasValue?AuditActorType.User:AuditActorType.PlatformUser):AuditActorType.System),e.Action,e.Category,e.EntityType,e.EntityId,e.EntityDisplayName,e.Outcome,Limit(e.Description,500),
            sanitizer.Serialize(e.Before),sanitizer.Serialize(e.After),sanitizer.Serialize(e.Metadata),http?.Items["SchoolERP.CorrelationId"]?.ToString()??http?.Response.Headers["X-Correlation-ID"].ToString(),http?.TraceIdentifier,Activity.Current?.TraceId.ToString(),e.SessionId??http?.User.FindFirstValue("session_id"),client.IpAddress,Limit(client.UserAgent,512),clock.GetUtcNow().UtcDateTime));
        await db.SaveChangesAsync(ct);
    }
    internal static string? Mask(string? email){if(string.IsNullOrWhiteSpace(email)||!email.Contains('@'))return null;var p=email.Split('@',2);return $"{p[0][0]}***@{p[1]}";}
    internal static string? Limit(string? value,int max)=>string.IsNullOrWhiteSpace(value)?null:value[..Math.Min(max,value.Length)];
}

public sealed class AuditQueryService(SchoolErpDbContext db, ICurrentUser user,
    Microsoft.Extensions.Options.IOptions<AuditLogOptions> options) : IAuditQueryService
{
    public async Task<PagedResult<AuditLogDto>> ListAsync(AuditLogQuery q,bool platform,CancellationToken ct=default)
    {
        var now=DateTime.UtcNow;var from=q.FromUtc??now.AddDays(-options.Value.DefaultQueryDays);var to=q.ToUtc??now;
        if(to<from||to-from>TimeSpan.FromDays(options.Value.MaxQueryRangeDays))throw new FluentValidation.ValidationException("Audit query date range is invalid or exceeds the configured maximum.");
        var page=Math.Max(1,q.PageNumber);var size=Math.Clamp(q.PageSize,1,100);var query=Scoped(platform).Where(x=>x.OccurredAtUtc>=from&&x.OccurredAtUtc<=to);
        if(!string.IsNullOrWhiteSpace(q.Category))query=query.Where(x=>x.Category==q.Category);if(!string.IsNullOrWhiteSpace(q.Action))query=query.Where(x=>x.Action==q.Action);if(q.Outcome.HasValue)query=query.Where(x=>x.Outcome==q.Outcome);if(!string.IsNullOrWhiteSpace(q.EntityType))query=query.Where(x=>x.EntityType==q.EntityType);
        if(!string.IsNullOrWhiteSpace(q.Search)){var s=q.Search.Trim().ToLower();query=query.Where(x=>x.Action.ToLower().Contains(s)||(x.EntityDisplayName!=null&&x.EntityDisplayName.ToLower().Contains(s))||(x.ActorDisplayName!=null&&x.ActorDisplayName.ToLower().Contains(s))||(x.CorrelationId!=null&&x.CorrelationId.ToLower().Contains(s)));}
        var count=await query.CountAsync(ct);var items=await query.OrderByDescending(x=>x.OccurredAtUtc).Skip((page-1)*size).Take(size).Select(Project()).ToListAsync(ct);return new(items,page,size,count);
    }
    public Task<AuditLogDto?> GetAsync(Guid id,bool platform,CancellationToken ct=default)=>Scoped(platform).Where(x=>x.Id==id).Select(Project()).SingleOrDefaultAsync(ct);
    private IQueryable<AuditLog> Scoped(bool platform){var q=db.AuditLogs.AsNoTracking();if(platform){if(user.TenantId.HasValue)throw new UnauthorizedAccessException();return q;}if(!user.TenantId.HasValue)throw new UnauthorizedAccessException();q=q.Where(x=>x.TenantId==user.TenantId);if(user.SchoolId.HasValue)q=q.Where(x=>x.SchoolId==user.SchoolId);if(user.CampusId.HasValue)q=q.Where(x=>x.CampusId==user.CampusId);return q;}
    private static System.Linq.Expressions.Expression<Func<AuditLog,AuditLogDto>> Project()=>x=>new(x.Id,x.TenantId,x.SchoolId,x.CampusId,x.ActorUserId,x.ActorDisplayName,x.ActorEmailMasked,x.ActorType,x.Action,x.Category,x.EntityType,x.EntityId,x.EntityDisplayName,x.Outcome,x.Description,x.BeforeJson,x.AfterJson,x.MetadataJson,x.CorrelationId,x.RequestId,x.TraceId,x.SessionId,x.IpAddress,x.UserAgent,x.OccurredAtUtc);
}
