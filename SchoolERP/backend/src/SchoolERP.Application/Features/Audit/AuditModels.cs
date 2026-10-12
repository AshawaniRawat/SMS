using SchoolERP.Application.Common.Pagination;
using SchoolERP.Domain.Enums;
namespace SchoolERP.Application.Features.Audit;

public sealed record AuditEntry(string Action, string Category, string? EntityType=null, string? EntityId=null,
    string? EntityDisplayName=null, AuditOutcome Outcome=AuditOutcome.Succeeded, string? Description=null,
    object? Before=null, object? After=null, object? Metadata=null, Guid? TenantId=null, Guid? SchoolId=null,
    Guid? CampusId=null, AuditActorType? ActorType=null, Guid? ActorUserId=null, string? SessionId=null);
public sealed record AuditLogQuery(DateTime? FromUtc=null, DateTime? ToUtc=null, string? Category=null,
    string? Action=null, AuditOutcome? Outcome=null, string? EntityType=null, string? Search=null,
    int PageNumber=1, int PageSize=50);
public sealed record AuditLogDto(Guid Id, Guid? TenantId, Guid? SchoolId, Guid? CampusId, Guid? ActorUserId,
    string? ActorDisplayName, string? ActorEmailMasked, AuditActorType ActorType, string Action, string Category,
    string? EntityType, string? EntityId, string? EntityDisplayName, AuditOutcome Outcome, string? Description,
    string? BeforeJson, string? AfterJson, string? MetadataJson, string? CorrelationId, string? RequestId,
    string? TraceId, string? SessionId, string? IpAddress, string? UserAgent, DateTime OccurredAtUtc);
public interface IAuditService { Task WriteAsync(AuditEntry entry, CancellationToken ct=default); }
public interface IAuditQueryService
{
    Task<PagedResult<AuditLogDto>> ListAsync(AuditLogQuery query, bool platform, CancellationToken ct=default);
    Task<AuditLogDto?> GetAsync(Guid id, bool platform, CancellationToken ct=default);
}
public interface IAuditDataSanitizer { string? Serialize(object? value); }
