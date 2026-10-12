namespace SchoolERP.Api.ErrorHandling;public sealed record ApiErrorDescriptor(int StatusCode,string Code,string Message,IDictionary<string,string[]>? Errors,LogLevel LogLevel,bool IsExpected=true);
