using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SchoolERP.Application.Features.Authentication;

namespace SchoolERP.IntegrationTests;

public sealed class SchoolErpWebApplicationFactory : ApiFactory
{
    public SchoolErpWebApplicationFactory(string connectionString)
        : base(connectionString, environment: "Testing", configureServices: services =>
        {
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService, FakeEmailService>();
        })
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["OpenApi:Enabled"] = "true",
                ["OpenApi:EnableUi"] = "false",
                ["Serilog:MinimumLevel:Default"] = "Warning"
            }));
    }

    public HttpClient CreateAnonymousClient() => CreateClient(new()
    {
        BaseAddress = new Uri("https://localhost")
    });

    public Task<HttpClient> CreateAuthenticatedClientAsync(TestUserSpec user,
        string? correlationId = null)
    {
        var client = CreateAnonymousClient().WithBearerToken(TestJwtTokenFactory.Create(user));
        if (!string.IsNullOrWhiteSpace(correlationId)) client.WithCorrelationId(correlationId);
        return Task.FromResult(client);
    }
}

public sealed class FakeEmailService : IEmailService
{
    public List<(string Email, string Link, string Kind)> Messages { get; } = [];

    public Task SendPasswordResetEmailAsync(string email, string resetLink,
        CancellationToken cancellationToken = default)
    {
        Messages.Add((email, resetLink, "password-reset"));
        return Task.CompletedTask;
    }

    public Task SendEmailVerificationAsync(string email, string verificationLink,
        CancellationToken cancellationToken = default)
    {
        Messages.Add((email, verificationLink, "email-verification"));
        return Task.CompletedTask;
    }
}
