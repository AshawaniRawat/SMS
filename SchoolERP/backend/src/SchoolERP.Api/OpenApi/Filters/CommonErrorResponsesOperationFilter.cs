using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using SchoolERP.Shared.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace SchoolERP.Api.OpenApi.Filters;
public sealed class CommonErrorResponsesOperationFilter:IOperationFilter
{
    public void Apply(OpenApiOperation operation,OperationFilterContext context)
    {
        var schema=context.SchemaGenerator.GenerateSchema(typeof(ApiErrorResponse),context.SchemaRepository);var authorized=context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<Microsoft.AspNetCore.Authorization.IAuthorizeData>().Any();
        Add(operation,"400","Invalid request or validation failure.",schema,Example("Validation.Failed","One or more validation errors occurred.",400,new JsonObject{{"name",new JsonArray("Name is required.")}}));
        if(authorized){Add(operation,"401","Authentication is required.",schema,Example("Authorization.Unauthenticated","Authentication is required.",401));Add(operation,"403","The caller lacks a required permission.",schema,Example("Authorization.PermissionDenied","You do not have permission to perform this action.",403));}
        Add(operation,"404","The route or scoped resource was not found.",schema,Example("Common.NotFound","The requested resource was not found.",404));Add(operation,"409","The operation conflicts with current state.",schema,Example("AcademicYear.DateOverlap","Academic year overlaps with an existing academic year.",409));Add(operation,"429","Rate limit exceeded.",schema,Example("RateLimit.Exceeded","Too many requests. Please try again later.",429));Add(operation,"500","Unexpected server error.",schema,Example("System.UnexpectedError","An unexpected error occurred.",500));Add(operation,"503","A required service is temporarily unavailable.",schema,Example("Infrastructure.DatabaseUnavailable","Database temporarily unavailable.",503));
    }
    private static void Add(OpenApiOperation op,string status,string description,IOpenApiSchema schema,JsonNode example){if(op.Responses?.ContainsKey(status)==true)return;op.Responses??=new OpenApiResponses();op.Responses[status]=new OpenApiResponse{Description=description,Content=new Dictionary<string,OpenApiMediaType>{{"application/json",new OpenApiMediaType{Schema=schema,Example=example}}}};}
    private static JsonObject Example(string code,string message,int status,JsonObject? errors=null)=>new(){{"success",false},{"code",code},{"message",message},{"status",status},{"correlationId","abc-123"},{"errors",errors}};
}
