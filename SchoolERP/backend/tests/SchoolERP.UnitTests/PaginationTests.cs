using SchoolERP.Application.Common.Pagination;
using Xunit;

namespace SchoolERP.UnitTests;

public sealed class PaginationTests
{
    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void InvalidRequestIsRejected(int pageNumber, int pageSize) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new PagedRequest(pageNumber, pageSize));

    [Fact]
    public void ResultCalculatesNavigationAndTotalPages()
    {
        var result = new PagedResult<int>([21, 22], 2, 20, 42);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void EmptyResultHasNoPagesOrNavigation()
    {
        var result = new PagedResult<int>([], 1, 20, 0);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }
}
