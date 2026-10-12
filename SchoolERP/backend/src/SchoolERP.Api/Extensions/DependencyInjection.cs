using SchoolERP.Api.Configuration;
using SchoolERP.Api.Middleware;
using System.Threading.RateLimiting;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SchoolERP.Application.Security;
using Microsoft.Extensions.Options;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using SchoolERP.Api.Security;
using SchoolERP.Api.Logging;
using SchoolERP.Api.ErrorHandling;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Api.OpenApi;
namespace SchoolERP.Api.Extensions;
public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers().ConfigureApiBehaviorOptions(options=>options.InvalidModelStateResponseFactory=context=>{var malformed=context.ModelState.Any(x=>x.Key=="$"&&x.Value?.Errors.Count>0)||context.ModelState.Values.SelectMany(x=>x.Errors).Any(x=>x.Exception is not null);var errors=context.ModelState.Where(x=>x.Value?.Errors.Count>0).ToDictionary(x=>string.IsNullOrEmpty(x.Key)?"body":char.ToLowerInvariant(x.Key[0])+x.Key[1..],x=>x.Value!.Errors.Select(e=>malformed?"The supplied value is invalid.":string.IsNullOrWhiteSpace(e.ErrorMessage)?"The supplied value is invalid.":e.ErrorMessage).Distinct().ToArray());var code=malformed?ErrorCodes.InvalidJson:ErrorCodes.ValidationFailed;var message=malformed?"Request body contains invalid JSON.":"One or more validation errors occurred.";return new BadRequestObjectResult(ApiErrorWriter.Create(context.HttpContext,code,message,400,errors));});
        services.AddSchoolErpOpenApi(configuration);
        services.AddProblemDetails();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected=async(context,ct)=>await ApiErrorWriter.WriteAsync(context.HttpContext,ApiErrorWriter.Create(context.HttpContext,ErrorCodes.RateLimit,"Too many requests. Please try again later.",429),ct);
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
        services.AddSingleton<IExceptionMapper,ApiExceptionMapper>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
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
