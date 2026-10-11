using SchoolERP.Infrastructure.Tenancy;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class PlatformExecutionScopeTests
{
    [Fact]
    public void Scope_is_explicit_nested_and_restored()
    {
        var scope = new PlatformExecutionScope();
        Assert.False(scope.IsEnabled);
        using (scope.Begin())
        {
            Assert.True(scope.IsEnabled);
            using (scope.Begin()) Assert.True(scope.IsEnabled);
            Assert.True(scope.IsEnabled);
        }
        Assert.False(scope.IsEnabled);
    }
}
