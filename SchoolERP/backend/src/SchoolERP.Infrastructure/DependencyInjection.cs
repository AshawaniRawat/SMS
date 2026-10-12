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
using SchoolERP.Application.Security;
using SchoolERP.Application.Features.Roles;
using SchoolERP.Application.Features.Permissions;
using SchoolERP.Application.Features.RolePermissions;
using SchoolERP.Application.Features.PlatformDashboard;
using SchoolERP.Infrastructure.PlatformDashboard;
using SchoolERP.Application.Features.SchoolDashboard;
using SchoolERP.Infrastructure.SchoolDashboard;
using SchoolERP.Application.Features.Audit;
using SchoolERP.Infrastructure.Audit;
namespace SchoolERP.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOptions<DatabaseOptions>().Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<IClientRequestContext, HttpClientRequestContext>();
        services.AddScoped<ICurrentTenant, CurrentTenant>();
        services.AddScoped<PlatformExecutionScope>();
        services.AddScoped<IPlatformDataAccess>(provider => provider.GetRequiredService<PlatformExecutionScope>());
        services.AddScoped<IPlatformExecutionScope>(provider => provider.GetRequiredService<PlatformExecutionScope>());
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddScoped<TenantGuardInterceptor>();
        services.AddScoped<ConcurrencyTokenInterceptor>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddOptions<AuditLogOptions>().Bind(configuration.GetSection(AuditLogOptions.SectionName)).Validate(x=>x.DefaultQueryDays>0&&x.MaxQueryRangeDays>=x.DefaultQueryDays&&x.MaxMetadataBytes is >=1024 and <=65536,"AuditLog options are invalid.").ValidateOnStart();
        services.AddSingleton<IAuditDataSanitizer,AuditDataSanitizer>();
        services.AddScoped<IAuditService,AuditService>();
        services.AddScoped<IAuditQueryService,AuditQueryService>();
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<PermissionSeeder>();
        services.AddScoped<RolePermissionSeeder>();
        services.AddScoped<DevelopmentAdminSeeder>();
        services.Configure<DevelopmentBootstrapOptions>(configuration.GetSection(DevelopmentBootstrapOptions.SectionName));
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
            options.AddInterceptors(provider.GetRequiredService<AuditSaveChangesInterceptor>());
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
            options.Tokens.EmailConfirmationTokenProvider = "EmailConfirmation";
        }).AddRoles<ApplicationRole>().AddEntityFrameworkStores<SchoolErpDbContext>().AddDefaultTokenProviders()
            .AddTokenProvider<EmailConfirmationTokenProvider<ApplicationUser>>("EmailConfirmation");
        services.AddScoped<IUserIdentityService, UserIdentityService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
        services.AddSingleton<IRefreshTokenHasher, RefreshTokenHasher>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<ISessionService, UserSessionService>();
        services.AddSingleton<IIdentityTokenEncoder, IdentityTokenEncoder>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        services.AddScoped<IRoleManagementService, RoleManagementService>();
        services.AddScoped<IPermissionCatalogService, PermissionCatalogService>();
        services.AddSingleton<IRolePermissionCompatibilityService, RolePermissionCompatibilityService>();
        services.AddScoped<IRolePermissionService, RolePermissionService>();
        services.AddScoped<IUserPermissionService, UserPermissionService>();
        services.AddScoped<IPlatformDashboardService, PlatformDashboardService>();
        services.AddScoped<ISchoolDashboardService, SchoolDashboardService>();
        services.Configure<PlatformDashboardOptions>(configuration.GetSection(PlatformDashboardOptions.SectionName));
        services.Configure<EmailDeliverySettings>(configuration.GetSection(EmailDeliverySettings.SectionName));
        services.AddOptions<DataProtectionTokenProviderOptions>()
            .Configure<IOptions<PasswordResetOptions>>((tokenOptions, passwordReset) =>
                tokenOptions.TokenLifespan = TimeSpan.FromMinutes(passwordReset.Value.TokenLifespanMinutes));
        services.AddOptions<EmailConfirmationTokenProviderOptions>()
            .Configure<IOptions<EmailVerificationOptions>>((tokenOptions, verification) =>
                tokenOptions.TokenLifespan = TimeSpan.FromHours(verification.Value.TokenLifespanHours));
        services.AddHealthChecks().AddCheck<PostgresHealthCheck>("postgresql", tags: ["ready"]);
        return services;
    }
}
