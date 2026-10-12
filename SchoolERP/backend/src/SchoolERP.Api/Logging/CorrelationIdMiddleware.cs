using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using SchoolERP.Api.Configuration;
namespace SchoolERP.Api.Logging;
public sealed partial class CorrelationIdMiddleware(RequestDelegate next,IOptions<SchoolErpLoggingOptions> options)
{
 public const string ItemKey="SchoolERP.CorrelationId";
 public async Task InvokeAsync(HttpContext context,ILogger<CorrelationIdMiddleware> logger)
 {
  var o=options.Value;var incoming=context.Request.Headers[o.CorrelationHeaderName].FirstOrDefault();var id=Valid(incoming,o.CorrelationIdMaxLength)?incoming!:Guid.NewGuid().ToString("N");
  context.Items[ItemKey]=id;context.Response.Headers[o.CorrelationHeaderName]=id;
  using(logger.BeginScope(new Dictionary<string,object?>{{"CorrelationId",id},{"RequestId",context.TraceIdentifier},{"TraceId",System.Diagnostics.Activity.Current?.TraceId.ToString()},{"SpanId",System.Diagnostics.Activity.Current?.SpanId.ToString()}})){await next(context);}
 }
 private static bool Valid(string? x,int max)=>!string.IsNullOrWhiteSpace(x)&&x.Length<=max&&Safe().IsMatch(x)&&!x.Any(char.IsControl);
 [GeneratedRegex("\\A[A-Za-z0-9._:-]+\\z",RegexOptions.CultureInvariant)]private static partial Regex Safe();
}
