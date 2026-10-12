using SchoolERP.Domain.Enums;

namespace SchoolERP.Domain.Entities;

public sealed class AuditLog
{
    private AuditLog() { }
    public Guid Id { get; private set; }
    public Guid? TenantId { get; private set; }
    public Guid? SchoolId { get; private set; }
    public Guid? CampusId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? ActorDisplayName { get; private set; }
    public string? ActorEmailMasked { get; private set; }
    public AuditActorType ActorType { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string? EntityType { get; private set; }
    public string? EntityId { get; private set; }
    public string? EntityDisplayName { get; private set; }
    public AuditOutcome Outcome { get; private set; }
    public string? Description { get; private set; }
    public string? BeforeJson { get; private set; }
    public string? AfterJson { get; private set; }
    public string? MetadataJson { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? RequestId { get; private set; }
    public string? TraceId { get; private set; }
    public string? SessionId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    public static AuditLog Create(Guid id, Guid? tenantId, Guid? schoolId, Guid? campusId, Guid? actorUserId,
        string? actorDisplayName, string? actorEmailMasked, AuditActorType actorType, string action, string category,
        string? entityType, string? entityId, string? entityDisplayName, AuditOutcome outcome, string? description,
        string? beforeJson, string? afterJson, string? metadataJson, string? correlationId, string? requestId,
        string? traceId, string? sessionId, string? ipAddress, string? userAgent, DateTime occurredAtUtc) => new()
        {
            Id=id, TenantId=tenantId, SchoolId=schoolId, CampusId=campusId, ActorUserId=actorUserId,
            ActorDisplayName=actorDisplayName, ActorEmailMasked=actorEmailMasked, ActorType=actorType,
            Action=action, Category=category, EntityType=entityType, EntityId=entityId,
            EntityDisplayName=entityDisplayName, Outcome=outcome, Description=description,
            BeforeJson=beforeJson, AfterJson=afterJson, MetadataJson=metadataJson, CorrelationId=correlationId,
            RequestId=requestId, TraceId=traceId, SessionId=sessionId, IpAddress=ipAddress,
            UserAgent=userAgent, OccurredAtUtc=DateTime.SpecifyKind(occurredAtUtc, DateTimeKind.Utc)
        };
}
