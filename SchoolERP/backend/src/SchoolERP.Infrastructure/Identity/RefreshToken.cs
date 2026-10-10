using SchoolERP.Domain.Common;
using SchoolERP.Domain.Exceptions;
namespace SchoolERP.Infrastructure.Identity;
public sealed class RefreshToken : AuditableEntity
{
    private RefreshToken() { }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? RevokedReason { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public Guid? ParentTokenId { get; private set; }
    public string? CreatedByIp { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? UserAgent { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }
    public Guid? TenantId { get; private set; }
    public Guid? SchoolId { get; private set; }
    public Guid? CampusId { get; private set; }
    public string SessionId { get; private set; } = string.Empty;
    public string SecurityStampHash { get; private set; } = string.Empty;
    public long Version { get; private set; }
    public ApplicationUser User { get; private set; } = null!;
    public RefreshToken? ParentToken { get; private set; }
    public RefreshToken? ReplacedByToken { get; private set; }
    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc.HasValue;
    public bool IsActive(DateTime utcNow) => !IsUsed && !IsRevoked && !IsExpired(utcNow);
    public static RefreshToken Create(Guid userId, string hash, DateTime expiresAtUtc, string sessionId,
        string securityStampHash, Guid? tenantId, Guid? schoolId, Guid? campusId, string? ip, string? userAgent,
        Guid? parentId = null) => new()
        {
            UserId = userId, TokenHash = hash, ExpiresAtUtc = Utc(expiresAtUtc), SessionId = sessionId,
            SecurityStampHash = securityStampHash, TenantId = tenantId, SchoolId = schoolId, CampusId = campusId,
            CreatedByIp = Trim(ip, 64), UserAgent = Trim(userAgent, 500), ParentTokenId = parentId, Version = 1
        };
    public void MarkUsed(DateTime utcNow, Guid replacementId)
    {
        if (IsUsed || IsRevoked) throw new DomainException("Refresh token is no longer active.");
        IsUsed = true; UsedAtUtc = Utc(utcNow); ReplacedByTokenId = replacementId; Version++;
    }
    public void Revoke(DateTime utcNow, string reason, string? ip)
    {
        if (IsRevoked) return;
        RevokedAtUtc = Utc(utcNow); RevokedReason = Trim(reason, 200); RevokedByIp = Trim(ip, 64); Version++;
    }
    private static string? Trim(string? value, int max) { var v = value?.Trim(); return string.IsNullOrEmpty(v) ? null : v[..Math.Min(v.Length, max)]; }
    private static DateTime Utc(DateTime value) => value.Kind switch { DateTimeKind.Utc => value, DateTimeKind.Local => value.ToUniversalTime(), _ => DateTime.SpecifyKind(value, DateTimeKind.Utc) };
}
