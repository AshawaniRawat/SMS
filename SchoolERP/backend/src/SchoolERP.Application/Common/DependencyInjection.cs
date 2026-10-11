using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SchoolERP.Application.Features.Tenants;
using SchoolERP.Application.Features.Schools;
using SchoolERP.Application.Features.Campuses;
using SchoolERP.Application.Features.AcademicYears;
using SchoolERP.Application.Security;
using SchoolERP.Application.Features.SchoolProfile;
using SchoolERP.Application.Features.SchoolClasses;
using SchoolERP.Application.Features.ClassSections;
using SchoolERP.Application.Features.Subjects;
using SchoolERP.Application.Features.Departments;
namespace SchoolERP.Application.Common;
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ISchoolService, SchoolService>();
        services.AddScoped<ICurrentSchoolResolver, CurrentSchoolResolver>();
        services.AddScoped<ISchoolProfileService, SchoolProfileService>();
        services.AddScoped<ISchoolClassService,SchoolClassService>();
        services.AddScoped<IClassSectionService,ClassSectionService>();
        services.AddScoped<ISubjectService,SubjectService>();
        services.AddScoped<IDepartmentService,DepartmentService>();
        services.AddScoped<ICampusService, CampusService>();
        services.AddScoped<IAcademicYearService, AcademicYearService>();
        services.AddScoped<IResourceScopeValidator, ResourceScopeValidator>();
        return services;
    }
}
