using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SchoolERP.Api.Extensions;
using SchoolERP.Application.Security;
using Xunit;

namespace SchoolERP.IntegrationTests;

public sealed class JwtConfigurationTests
{
    [Theory]
    [InlineData("Jwt:SigningKey", "")]
    [InlineData("Jwt:SigningKey", "short")]
    [InlineData("Jwt:SigningKey", "SET_USING_ENVIRONMENT_OR_SECRET_STORE")]
    [InlineData("Jwt:Issuer", "")]
    [InlineData("Jwt:Audience", "")]
    [InlineData("Jwt:AccessTokenLifetimeMinutes", "2")]
    [InlineData("Jwt:ClockSkewSeconds", "301")]
    public void Invalid_jwt_configuration_is_rejected(string key, string value)
    {
        var values = ValidValues(); values[key] = value;
        using var provider = Services(values);
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<JwtOptions>>().Value);
    }

    [Fact]
    public void Valid_external_configuration_is_accepted()
    {
        using var provider = Services(ValidValues());
        Assert.Equal(15, provider.GetRequiredService<IOptions<JwtOptions>>().Value.AccessTokenLifetimeMinutes);
    }

    private static ServiceProvider Services(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection(); services.AddLogging(); services.AddApiServices(configuration);
        return services.BuildServiceProvider();
    }
    private static Dictionary<string, string?> ValidValues() => new()
    {
        ["Jwt:Issuer"] = "SchoolERP.Tests", ["Jwt:Audience"] = "SchoolERP.Client",
        ["Jwt:SigningKey"] = "configuration-test-only-key-with-at-least-32-bytes",
        ["Jwt:AccessTokenLifetimeMinutes"] = "15", ["Jwt:ClockSkewSeconds"] = "30"
    };
}
