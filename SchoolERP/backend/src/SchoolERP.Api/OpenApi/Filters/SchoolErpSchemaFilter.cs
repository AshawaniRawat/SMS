using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace SchoolERP.Api.OpenApi.Filters;
public sealed class SchoolErpSchemaFilter:ISchemaFilter
{
    public void Apply(IOpenApiSchema schema,SchemaFilterContext context)
    {
        if(schema is OpenApiSchema concrete){if(context.Type==typeof(DateOnly)||context.Type==typeof(DateOnly?)){concrete.Type=JsonSchemaType.String;concrete.Format="date";}if(context.Type==typeof(TimeOnly)||context.Type==typeof(TimeOnly?)){concrete.Type=JsonSchemaType.String;concrete.Format="time";}}
        if(schema.Properties is null)return;foreach(var p in schema.Properties){if(p.Value is not OpenApiSchema property)continue;if(p.Key.Contains("password",StringComparison.OrdinalIgnoreCase)||p.Key.Contains("token",StringComparison.OrdinalIgnoreCase))property.WriteOnly=true;if(p.Key.Equals("colorHex",StringComparison.OrdinalIgnoreCase)){property.Pattern="^#[0-9A-Fa-f]{6}$";property.Example="#EF4444";}}
    }
}
