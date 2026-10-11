using SchoolERP.Application.Abstractions;

namespace SchoolERP.Infrastructure.Tenancy;

public sealed class PlatformExecutionScope : IPlatformDataAccess, IPlatformExecutionScope
{
    private int depth;
    public bool IsEnabled => depth > 0;
    public IDisposable Begin() { depth++; return new Lease(this); }
    private sealed class Lease(PlatformExecutionScope owner) : IDisposable
    {
        private bool disposed;
        public void Dispose() { if (disposed) return; disposed = true; owner.depth--; }
    }
}
