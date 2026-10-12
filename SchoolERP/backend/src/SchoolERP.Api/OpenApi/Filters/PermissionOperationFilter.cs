using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using SchoolERP.Api.Security;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace SchoolERP.Api.OpenApi.Filters;
public sealed class PermissionOperationFilter:IOperationFilter
{
    public void Apply(OpenApiOperation operation,OperationFilterContext context)
    {
        var permissions=context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<RequirePermissionAttribute>().Select(x=>x.Policy![PermissionPolicy.Prefix.Length..]).Distinct().ToArray();if(permissions.Length==0)return;
        operation.Extensions??=new Dictionary<string,IOpenApiExtension>();var values=new JsonArray();foreach(var permission in permissions)values.Add(permission);operation.Extensions["x-required-permissions"]=new JsonNodeExtension(values);
        var note=$"Required permission{(permissions.Length>1?"s":"")}: {string.Join(", ",permissions)}.";operation.Description=string.IsNullOrWhiteSpace(operation.Description)?note:$"{operation.Description}\n\n{note}";
    }
}
