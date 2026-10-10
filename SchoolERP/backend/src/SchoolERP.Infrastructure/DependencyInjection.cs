using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolERP.Infrastructure.Persistence.Context;
using Microsoft.Extensions.Options;
using SchoolERP.Application.Abstractions;
using SchoolERP.Infrastructure.Persistence;
using SchoolERP.Infrastructure.Persistence.Interceptors;
using SchoolERP.Infrastructure.Persistence.Seed;
using SchoolERP.Infrastructure.Tenancy;
using SchoolERP.Infrastructure.Health;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity;
using SchoolERP.Application.Features.Identity;
using SchoolERP.Infrastructure.Identity;
using SchoolERP.Application.Features.Authentication;
namespace SchoolERP.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOptions<DatabaseOptions>().Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddScoped<ICurrentTenant, CurrentTenant>();
        services.AddScoped<IPlatformDataAccess, RestrictedPlatformDataAccess>();
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddScoped<TenantGuardInterceptor>();
        services.AddScoped<ConcurrencyTokenInterceptor>();
        services.AddScoped<DatabaseSeeder>();
        services.AddSingleton(TimeProvider.System);
        var identitySettings = configuration.GetSection(IdentitySettings.SectionName).Get<IdentitySettings>() ?? new IdentitySettings();
        services.Configure<IdentitySettings>(configuration.GetSection(IdentitySettings.SectionName));
        services.AddDataProtection();
        services.AddDbContext<SchoolErpDbContext>((provider, options) =>
        {
            var database = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var connectionString = configuration.GetConnectionString(database.ConnectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Database connection is not configured. Supply ConnectionStrings__DefaultConnection through configuration.");
            options.UseNpgsql(connectionString, postgres => postgres
                .CommandTimeout(database.CommandTimeoutSeconds)
                .EnableRetryOnFailure(database.MaxRetryCount, TimeSpan.FromSeconds(database.MaxRetryDelaySeconds), null));
            options.AddInterceptors(provider.GetRequiredService<AuditableEntityInterceptor>());
            options.AddInterceptors(
                provider.GetRequiredService<TenantGuardInterceptor>(),
                provider.GetRequiredService<SoftDeleteInterceptor>(),
                provider.GetRequiredService<ConcurrencyTokenInterceptor>());
            if (environment.IsDevelopment() && database.EnableSensitiveDataLogging)
                options.EnableSensitiveDataLogging();
            if (environment.IsDevelopment() && database.EnableDetailedErrors)
                options.EnableDetailedErrors();
        });
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<SchoolErpDbContext>());
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<SchoolErpDbContext>());
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = identitySettings.PasswordRequiredLength;
            options.Password.RequiredUniqueChars = identitySettings.PasswordRequiredUniqueChars;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = identitySettings.MaxFailedAccessAttempts;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(identitySettings.DefaultLockoutMinutes);
            options.SignIn.RequireConfirmedEmail = identitySettings.RequireConfirmedEmail;
        }).AddRoles<ApplicationRole>().AddEntityFrameworkStores<SchoolErpDbContext>().AddDefaultTokenProviders();
        services.AddScoped<IUserIdentityService, UserIdentityService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddHealthChecks().AddCheck<PostgresHealthCheck>("postgresql", tags: ["ready"]);
        return services;
    }
}
