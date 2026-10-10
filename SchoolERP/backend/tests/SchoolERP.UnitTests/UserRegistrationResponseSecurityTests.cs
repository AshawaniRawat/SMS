using System.Text.Json;
using SchoolERP.Application.Features.Identity;
using SchoolERP.Domain.Enums;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class UserRegistrationResponseSecurityTests
{
    [Fact]
    public void User_summary_serialization_excludes_identity_secrets()
    {
        var dto = new UserSummaryDto(Guid.NewGuid(), "Ada", "Lovelace", "ada@example.com", null, null,
            null, null, null, UserAccountStatus.Pending, false, false, DateTime.UtcNow);
        var json = JsonSerializer.Serialize(dto);
        Assert.DoesNotContain("Password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SecurityStamp", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConcurrencyStamp", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AccessFailedCount", json, StringComparison.OrdinalIgnoreCase);
    }
}
