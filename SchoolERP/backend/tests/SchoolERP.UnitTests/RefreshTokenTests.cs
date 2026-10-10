using SchoolERP.Infrastructure.Identity;
using SchoolERP.Domain.Exceptions;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class RefreshTokenTests
{
    [Fact]
    public void Hasher_generates_high_entropy_opaque_tokens_and_deterministic_hashes()
    {
        var hasher = new RefreshTokenHasher();
        var first = hasher.GenerateToken();
        var second = hasher.GenerateToken();

        Assert.NotEqual(first, second);
        Assert.True(first.Length >= 80);
        Assert.Equal(64, hasher.Hash(first).Length);
        Assert.Equal(hasher.Hash(first), hasher.Hash(first));
        Assert.NotEqual(first, hasher.Hash(first));
    }

    [Fact]
    public void Token_tracks_rotation_and_revocation_state()
    {
        var now = DateTime.UtcNow;
        var token = RefreshToken.Create(Guid.NewGuid(), new string('A', 64), now.AddDays(7), "session",
            new string('B', 64), null, null, null, "127.0.0.1", "tests");
        var replacementId = Guid.NewGuid();

        Assert.True(token.IsActive(now));
        token.MarkUsed(now.AddMinutes(1), replacementId);
        Assert.True(token.IsUsed);
        Assert.Equal(replacementId, token.ReplacedByTokenId);
        Assert.False(token.IsActive(now.AddMinutes(1)));
        Assert.Throws<DomainException>(() => token.MarkUsed(now.AddMinutes(2), Guid.NewGuid()));

        token.Revoke(now.AddMinutes(2), "reuse", "127.0.0.1");
        Assert.True(token.IsRevoked);
        Assert.Equal("reuse", token.RevokedReason);
    }

    [Fact]
    public void Expired_token_is_not_active()
    {
        var now = DateTime.UtcNow;
        var token = RefreshToken.Create(Guid.NewGuid(), new string('A', 64), now.AddSeconds(-1), "session",
            new string('B', 64), null, null, null, null, null);
        Assert.True(token.IsExpired(now));
        Assert.False(token.IsActive(now));
    }
}
