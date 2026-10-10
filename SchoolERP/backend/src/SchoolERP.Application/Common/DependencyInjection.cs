using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SchoolERP.Application.Features.Tenants;
namespace SchoolERP.Application.Common;
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<ITenantService, TenantService>();
        return services;
    }
}
