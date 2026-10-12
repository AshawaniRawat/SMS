using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace SchoolERP.Api.OpenApi.Filters;
public sealed class AuthorizeOperationFilter:IOperationFilter
{
    public void Apply(OpenApiOperation operation,OperationFilterContext context)
    {
        var metadata=context.ApiDescription.ActionDescriptor.EndpointMetadata;if(metadata.OfType<IAllowAnonymous>().Any())return;if(!metadata.OfType<IAuthorizeData>().Any())return;
        operation.Security??=[];operation.Security.Add(new OpenApiSecurityRequirement{[new OpenApiSecuritySchemeReference("Bearer",context.Document)]=[]});
    }
}
