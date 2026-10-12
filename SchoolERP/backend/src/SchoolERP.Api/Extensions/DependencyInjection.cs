using SchoolERP.Api.Configuration;
using SchoolERP.Api.Middleware;
using System.Threading.RateLimiting;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SchoolERP.Application.Security;
using Microsoft.Extensions.Options;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using SchoolERP.Api.Security;
using SchoolERP.Api.Logging;
namespace SchoolERP.Api.Extensions;
public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SchoolERP API",
                Version = "v1",
                Description = "Backend API for SchoolERP. Use the login endpoint to obtain a JWT, then select Authorize and enter the token."
            });
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
                Description = "Enter the access token issued by POST /api/v1/auth/login."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
            });
        });
        services.AddProblemDetails();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
            options.AddPolicy("refresh", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
            options.AddPolicy("forgot-password", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5, Window = TimeSpan.FromMinutes(15), QueueLimit = 0, AutoReplenishment = true
                }));
            options.AddPolicy("reset-password", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10, Window = TimeSpan.FromMinutes(15), QueueLimit = 0, AutoReplenishment = true
                }));
            options.AddPolicy("resend-email-verification", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5, Window = TimeSpan.FromMinutes(15), QueueLimit = 0, AutoReplenishment = true
                }));
            options.AddPolicy("confirm-email", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20, Window = TimeSpan.FromMinutes(15), QueueLimit = 0, AutoReplenishment = true
                }));
        });
        services.AddExceptionHandler<PersistenceExceptionHandler>();
        services.AddExceptionHandler<DatabaseExceptionHandler>();
        services.AddExceptionHandler<UnhandledExceptionHandler>();
        services.AddOptions<SchoolErpLoggingOptions>().Bind(configuration.GetSection(SchoolErpLoggingOptions.SectionName)).ValidateDataAnnotations().Validate(x=>!string.IsNullOrWhiteSpace(x.CorrelationHeaderName)&&!x.CorrelationHeaderName.Any(char.IsControl),"Correlation header name is invalid.").ValidateOnStart();
        services.AddScoped<ICorrelationContext,HttpCorrelationContext>();
        services.AddSingleton<ILogRedactor,LogRedactor>();
        services.AddHealthChecks();
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.AddOptions<JwtOptions>().Bind(jwtSection)
            .Validate(x => !string.IsNullOrWhiteSpace(x.Issuer), "Jwt:Issuer is required.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Audience), "Jwt:Audience is required.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.SigningKey) && Encoding.UTF8.GetByteCount(x.SigningKey) >= 32,
                "Jwt:SigningKey must contain at least 32 bytes.")
            .Validate(x => !x.SigningKey.Contains("SET_USING_", StringComparison.OrdinalIgnoreCase), "Jwt:SigningKey cannot be a placeholder.")
            .Validate(x => x.AccessTokenLifetimeMinutes is >= 5 and <= 60, "JWT access-token lifetime must be between 5 and 60 minutes.")
            .Validate(x => x.ClockSkewSeconds is >= 0 and <= 300, "JWT clock skew must be between 0 and 300 seconds.")
            .ValidateOnStart();
        services.AddOptions<RefreshTokenOptions>().Bind(configuration.GetSection(RefreshTokenOptions.SectionName))
            .Validate(x => x.RefreshTokenLifetimeDays is >= 1 and <= 30,
                "Refresh-token lifetime must be between 1 and 30 days.")
            .Validate(x => x.RefreshTokenRetentionDays is >= 0 and <= 365,
                "Refresh-token retention must be between 0 and 365 days.")
            .ValidateOnStart();
        services.AddOptions<PasswordResetOptions>().Bind(configuration.GetSection(PasswordResetOptions.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<FrontendOptions>().Bind(configuration.GetSection(FrontendOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(x => Uri.TryCreate(x.BaseUrl, UriKind.Absolute, out var uri) &&
                uri.Scheme is "http" or "https", "Frontend:BaseUrl must be an absolute HTTP(S) URL.")
            .ValidateOnStart();
        services.AddOptions<EmailVerificationOptions>()
            .Bind(configuration.GetSection(EmailVerificationOptions.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, configured) =>
        {
            var jwt = configured.Value;
            options.MapInboundClaims = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = jwt.Issuer,
                ValidateAudience = true, ValidAudience = jwt.Audience,
                ValidateLifetime = true, RequireExpirationTime = true,
                ValidateIssuerSigningKey = true, RequireSignedTokens = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds),
                NameClaimType = ClaimTypes.NameIdentifier, RoleClaimType = ClaimTypes.Role
            };
        });
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiAuthorizationResultHandler>();
        services.Configure<CorsOptions>(configuration.GetSection(CorsOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy("Frontend", policy =>
        {
            if (origins.Length > 0) policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
        }));
        // TODO: Register JWT authentication after issuer, audience and key validation is implemented.
        return services;
    }
}
