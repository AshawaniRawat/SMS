# Persistence architecture

`IApplicationDbContext` is the Application-layer EF boundary for the current foundation entities. `IUnitOfWork` exposes only asynchronous save behavior. `SchoolErpDbContext` implements both; no generic repository is used because it would hide useful EF Core query capabilities.

## Query guidance

Read-only queries use `AsNoTracking()` and project only the columns required by the caller:

```csharp
var schools = await dbContext.Schools
    .AsNoTracking()
    .Select(school => new { school.Id, school.Name })
    .ToListAsync(cancellationToken);
```

- Project instead of loading full graphs.
- Avoid N+1 query patterns and long `Include` chains.
- Paginate large lists with `ToPagedResultAsync`; page size is limited to 100.
- Pass `CancellationToken` to every asynchronous database operation.
- Inspect generated SQL and query plans for slow queries.
- Do not globally disable tracking; commands need tracked entities.

## Save pipeline

Scoped interceptors run tenant write protection, soft deletion, optimistic-concurrency version updates, and UTC auditing. A missing tenant rejects tenant-owned writes. An empty tenant ID on a new entity is assigned from the validated tenant context; an explicitly mismatched ID is rejected. Platform access is an internal authorization extension point and is never enabled from request headers.

`TimeProvider.System` supplies persistence timestamps and can be replaced in tests. Current-user infrastructure will later populate `CreatedBy`, `UpdatedBy`, and `DeletedBy`.

School demonstrates optimistic concurrency with a numeric `version` token. Conflicts are translated to `ConcurrencyConflictException`; the API maps that exception to HTTP 409 without exposing provider details.

## Transactions and converters

A single `SaveChangesAsync` call is already transactional. Add explicit transaction methods only when a future use case truly spans multiple saves. Future value-object converters belong in `Infrastructure/Persistence/Converters`; no speculative converters are included in P1-03.
