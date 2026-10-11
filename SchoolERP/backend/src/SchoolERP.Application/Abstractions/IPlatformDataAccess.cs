namespace SchoolERP.Application.Abstractions;

// Infrastructure extension point. Never enable directly from request headers or input.
// A future implementation must verify platform-admin authorization before granting access.
public interface IPlatformDataAccess
{
    bool IsEnabled { get; }
}

public interface IPlatformExecutionScope
{
    IDisposable Begin();
}
