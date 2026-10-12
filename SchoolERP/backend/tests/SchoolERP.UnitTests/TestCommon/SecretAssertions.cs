using Xunit;
namespace SchoolERP.UnitTests.TestCommon;
public static class SecretAssertions
{
    private static readonly string[] Secrets=["TestPassword123!","raw-refresh-token","test-signing-key"];
    public static void ShouldNotContainSecrets(this string value){foreach(var secret in Secrets)Assert.DoesNotContain(secret,value,StringComparison.Ordinal);}
}
