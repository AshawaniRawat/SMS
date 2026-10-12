using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace SchoolERP.Api.OpenApi.Filters;
public sealed class OperationMetadataFilter:IOperationFilter
{
    private static readonly Dictionary<string,string> Tags=new(StringComparer.OrdinalIgnoreCase){{"Auth","Authentication"},{"PlatformDashboard","Platform"},{"PlatformAuditLogs","Platform Audit"},{"AuditLogs","Audit"},{"AcademicYears","Academic Years"},{"SchoolClasses","Classes"},{"ClassSections","Sections"},{"ClassSectionHierarchy","Sections"},{"ClassSubjects","Subjects"},{"SchoolCalendar","Calendar"},{"RolePermissions","Roles"},{"UserRegistration","Users"},{"SchoolProfile","Schools"},{"SchoolDashboard","Schools"}};
    public void Apply(OpenApiOperation operation,OperationFilterContext context)
    {
        var controller=context.ApiDescription.ActionDescriptor.RouteValues["controller"]??"SchoolERP";operation.Tags=new HashSet<OpenApiTagReference>{new(Tags.GetValueOrDefault(controller,controller),context.Document)};
        foreach(var p in operation.Parameters??[]){if(p.Name?.Equals("pageNumber",StringComparison.OrdinalIgnoreCase)==true)p.Description="1-based page number.";else if(p.Name?.Equals("pageSize",StringComparison.OrdinalIgnoreCase)==true)p.Description=controller.Contains("Audit",StringComparison.OrdinalIgnoreCase)?"Page size from 1 to 100; default 50.":"Page size from 1 to 100; default 20.";else if(p.Name?.Equals("sortBy",StringComparison.OrdinalIgnoreCase)==true)p.Description=controller switch{"Campuses"=>"Sort field: Name, Code, Status, CreatedAt, or City.",_=>"Feature-supported sort field; see endpoint description."};}
    }
}
