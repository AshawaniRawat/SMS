using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using SchoolERP.Application.Features.Audit;
namespace SchoolERP.Infrastructure.Audit;
public sealed class AuditDataSanitizer(IOptions<AuditLogOptions> options) : IAuditDataSanitizer
{
    private static readonly HashSet<string> Blocked=new(StringComparer.OrdinalIgnoreCase)
    { "password","passwordHash","token","accessToken","refreshToken","resetToken","verificationToken","authorization","cookie","secret","apiKey","signingKey","connectionString","securityStamp" };
    private static readonly JsonSerializerOptions JsonOptions=new(JsonSerializerDefaults.Web);
    public string? Serialize(object? value)
    {
        if(value is null)return null;
        var node=JsonSerializer.SerializeToNode(value,JsonOptions); Sanitize(node);
        var json=node?.ToJsonString(JsonOptions); if(json is null)return null;
        return Encoding.UTF8.GetByteCount(json)<=options.Value.MaxMetadataBytes ? json : JsonSerializer.Serialize(new { truncated=true, reason="Audit data exceeded the configured size limit." },JsonOptions);
    }
    private static void Sanitize(JsonNode? node)
    {
        if(node is JsonObject obj){foreach(var key in obj.Select(x=>x.Key).ToArray()){if(Blocked.Any(x=>key.Contains(x,StringComparison.OrdinalIgnoreCase)))obj.Remove(key);else Sanitize(obj[key]);}}
        else if(node is JsonArray array)foreach(var child in array)Sanitize(child);
    }
}
