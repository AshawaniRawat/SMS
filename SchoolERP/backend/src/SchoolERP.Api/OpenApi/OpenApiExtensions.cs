using System.Reflection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using SchoolERP.Api.OpenApi.Filters;
namespace SchoolERP.Api.OpenApi;
public static class OpenApiExtensions
{
    public static IServiceCollection AddSchoolErpOpenApi(this IServiceCollection services,IConfiguration configuration)
    {
        services.AddOptions<SchoolErpOpenApiOptions>().Bind(configuration.GetSection(SchoolErpOpenApiOptions.SectionName))
            .Validate(x=>!string.IsNullOrWhiteSpace(x.RoutePrefix),"OpenApi route prefix is required.").ValidateOnStart();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options=>
        {
            options.SwaggerDoc("v1",new OpenApiInfo{Title="SchoolERP API v1",Version="v1",Description="Multi-tenant School ERP SaaS API for tenant, school, academic, identity, configuration and administration workflows. Tenant context is derived from the authenticated JWT; resource identifiers are resolved within the current tenant and school scope. Foreign-scope identifiers return not found. Error correlation IDs can be supplied to support."});
            options.AddSecurityDefinition("Bearer",new OpenApiSecurityScheme{Type=SecuritySchemeType.Http,Scheme="bearer",BearerFormat="JWT",Description="Enter a valid JWT access token. Do not enter a refresh token or include the Bearer prefix."});
            options.OperationFilter<RequestBodyOperationFilter>();options.OperationFilter<AuthorizeOperationFilter>();options.OperationFilter<PermissionOperationFilter>();options.OperationFilter<CommonErrorResponsesOperationFilter>();options.OperationFilter<OperationMetadataFilter>();
            options.SchemaFilter<SchoolErpSchemaFilter>();
            options.DocumentFilter<CanonicalPathsDocumentFilter>();
            options.CustomSchemaIds(t=>SchemaId(t));
            options.CustomOperationIds(api=>$"{api.ActionDescriptor.RouteValues["controller"]}_{api.ActionDescriptor.RouteValues["action"]}");
            var xml=Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,Assembly.GetExecutingAssembly().GetName().Name+".xml");if(File.Exists(xml))options.IncludeXmlComments(xml);
        });
        return services;
    }
    public static WebApplication UseSchoolErpOpenApi(this WebApplication app)
    {
        var settings=app.Services.GetRequiredService<IOptions<SchoolErpOpenApiOptions>>().Value;if(!settings.Enabled)return app;
        app.UseSwagger();if(settings.EnableUi)app.UseSwaggerUI(o=>{o.RoutePrefix=settings.RoutePrefix.Trim('/');o.SwaggerEndpoint("/swagger/v1/swagger.json","SchoolERP API v1");o.DocumentTitle="SchoolERP API Documentation";o.DisplayRequestDuration();o.EnablePersistAuthorization();});return app;
    }
    private static string SchemaId(Type type){if(!type.IsGenericType)return type.FullName?.Replace("+",".")??type.Name;var name=type.GetGenericTypeDefinition().FullName!.Split('`')[0].Replace("+",".");return name+"Of"+string.Join("And",type.GetGenericArguments().Select(SchemaId));}
}
