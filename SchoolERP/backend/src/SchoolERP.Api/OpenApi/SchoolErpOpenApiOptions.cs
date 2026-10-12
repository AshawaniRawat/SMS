namespace SchoolERP.Api.OpenApi;
public sealed class SchoolErpOpenApiOptions
{
    public const string SectionName="OpenApi";
    public bool Enabled { get; set; }
    public bool EnableUi { get; set; }
    public string RoutePrefix { get; set; }="swagger";
}
