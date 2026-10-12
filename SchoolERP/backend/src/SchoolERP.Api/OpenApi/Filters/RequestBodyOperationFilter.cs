using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace SchoolERP.Api.OpenApi.Filters;
public sealed class RequestBodyOperationFilter:IOperationFilter
{
    public void Apply(OpenApiOperation operation,OperationFilterContext context)
    {
        if(context.ApiDescription.HttpMethod is "GET" or "DELETE")return;
        var parameter=context.MethodInfo.GetParameters().FirstOrDefault(x=>x.ParameterType.IsClass&&x.ParameterType.Name.EndsWith("Request",StringComparison.Ordinal)&&x.ParameterType!=typeof(string));if(parameter is null)return;
        var schema=context.SchemaGenerator.GenerateSchema(parameter.ParameterType,context.SchemaRepository);operation.RequestBody=new OpenApiRequestBody{Required=true,Content=new Dictionary<string,OpenApiMediaType>{{"application/json",new OpenApiMediaType{Schema=schema,Example=Example(parameter.ParameterType.Name)}}}};
        var names=parameter.ParameterType.GetProperties().Select(x=>x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);if(operation.Parameters is not null)operation.Parameters=new List<IOpenApiParameter>(operation.Parameters.Where(x=>x.Name is null||!names.Contains(x.Name)));
    }
    private static JsonNode? Example(string name)=>name switch
    {
        "LoginRequest"=>new JsonObject{{"email","admin@example.com"},{"password","ExampleOnly123!"}},
        "RefreshTokenRequest"=>new JsonObject{{"refreshToken","example-refresh-token"}},
        "CreateCampusRequest"=>new JsonObject{{"name","Main Campus"},{"code","MAIN"},{"isMainCampus",true}},
        "CreateAcademicYearRequest"=>new JsonObject{{"name","2026-2027"},{"code","AY2627"},{"startDate","2026-06-01"},{"endDate","2027-05-31"}},
        "CreateSchoolRequest"=>new JsonObject{{"name","Example School"},{"code","EXAMPLE"}},
        "CreateSchoolClassRequest"=>new JsonObject{{"name","Grade 6"},{"code","G6"}},
        "CreateClassSectionRequest"=>new JsonObject{{"name","Section A"},{"code","A"}},
        "CreateSubjectRequest"=>new JsonObject{{"name","Mathematics"},{"code","MATH"}},
        "CreateDepartmentRequest"=>new JsonObject{{"name","Science"},{"code","SCI"}},
        "CreateHouseRequest"=>new JsonObject{{"name","Emerald"},{"code","EMR"},{"colorHex","#10B981"}},
        "CalendarEventRequest"=>new JsonObject{{"title","Founders Day"},{"code","FOUNDERS_DAY"},{"type",1},{"startDate","2026-09-15"},{"endDate","2026-09-15"},{"isAllDay",true},{"colorHex","#EF4444"}},
        _=>null
    };
}
