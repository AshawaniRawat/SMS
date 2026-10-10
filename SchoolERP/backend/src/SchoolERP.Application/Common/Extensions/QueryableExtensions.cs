using Microsoft.EntityFrameworkCore;
using SchoolERP.Application.Common.Pagination;

namespace SchoolERP.Application.Common.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, System.Linq.Expressions.Expression<Func<T, bool>> predicate) =>
        condition ? query.Where(predicate) : query;

    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, PagedRequest request) =>
        query.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize);

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.ApplyPaging(request).ToListAsync(cancellationToken);
        return new PagedResult<T>(items, request.PageNumber, request.PageSize, totalCount);
    }
}
