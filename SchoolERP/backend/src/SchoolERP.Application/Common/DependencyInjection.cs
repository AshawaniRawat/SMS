using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SchoolERP.Application.Features.Tenants;
using SchoolERP.Application.Features.Schools;
using SchoolERP.Application.Features.Campuses;
using SchoolERP.Application.Features.AcademicYears;
using SchoolERP.Application.Security;
namespace SchoolERP.Application.Common;
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ISchoolService, SchoolService>();
        services.AddScoped<ICampusService, CampusService>();
        services.AddScoped<IAcademicYearService, AcademicYearService>();
        services.AddScoped<IResourceScopeValidator, ResourceScopeValidator>();
        return services;
    }
}
