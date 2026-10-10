# Database conventions

- PostgreSQL through Npgsql is the only configured relational provider.
- Tables, columns, keys, constraints, and indexes use `snake_case` names.
- Entity identifiers are application-generated `Guid` values.
- Audit and deletion timestamps are UTC `timestamp with time zone` values. Clients display them in the user or school timezone; timezone business rules are deferred.
- Important strings have explicit entity-specific lengths. Unlimited text is used only deliberately.
- Decimal and nullable-decimal properties default to precision `18,2`; explicit property configuration may override it.
- Tenant-owned rows require `tenant_id`. Reads combine tenant isolation and soft-delete predicates into one global filter. Missing tenant context returns no tenant-owned rows.
- School and Campus opt into soft deletion. Tenant uses its explicit `Archived` lifecycle status instead, avoiding two competing deletion states. Deleted school/campus codes remain reserved because unique indexes do not include `is_deleted`.
- Tenant-to-School and School-to-Campus foreign keys use restrictive deletion. Normal workflows do not cascade-delete tenant data.
- Tenant slug is globally unique. School code is unique per tenant. Campus code is unique per school. Foreign-key indexes support tenant and parent lookups.
- School uses a numeric optimistic-concurrency `version` token. Provider concurrency exceptions are translated before reaching API clients.
- Migrations live in Infrastructure. `EnsureCreated` is forbidden. Development may apply committed migrations at startup; production must deploy them explicitly.
- Read queries use `AsNoTracking`, projections, cancellation tokens, and pagination where applicable.
- `IApplicationDbContext` and `IUnitOfWork` form the persistence boundary. Do not introduce a generic repository.
- A `SaveChangesAsync` call is the default transaction boundary. Explicit transactions are reserved for future multi-save workflows.
