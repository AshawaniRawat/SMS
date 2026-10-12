using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace SchoolERP.Api.OpenApi.Filters;
public sealed class CanonicalPathsDocumentFilter:IDocumentFilter
{
    public void Apply(OpenApiDocument document,DocumentFilterContext context)
    {
        if(document.Paths is null)return;foreach(var item in document.Paths.Where(x=>x.Key.Any(char.IsUpper)).ToArray()){document.Paths.Remove(item.Key);document.Paths[item.Key.ToLowerInvariant()]=item.Value;}
    }
}
