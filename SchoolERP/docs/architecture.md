# Architecture

The initial solution is a modular monolith with compile-time dependency boundaries: API → Application, Infrastructure, Shared; Application → Domain, Shared; Infrastructure → Application, Domain, Shared; Domain → Shared.

Feature folders are organization boundaries, not independently deployed services. Application owns use cases and ports. Domain owns business rules. Infrastructure owns EF Core, identity integration and external services. API owns HTTP configuration. Shared remains small and framework independent.

BaseEntity has audit fields without TenantId. School and Campus implement ITenantEntity. ICurrentTenant currently returns no tenant; tenant-owned query filters therefore return no rows. Each DbContext captures its own tenant abstraction and exposes properties referenced by the cached EF model. Named TenantIsolation filters are applied to tenant entity roots. SaveChanges rejects missing/mismatched tenant writes and tenant reassignment. This is an EF foundation, not PostgreSQL row-level security or authentication. Filters do not protect raw SQL, IgnoreQueryFilters, tracked entities already loaded in a different scope or direct database access. Never reuse contexts across tenant boundaries.

IPlatformDataAccess is the future privileged bypass extension. Its default implementation always denies bypass; no client input enables it. Only a future authorization-checked internal service may enable platform reads/writes. Tests use a test-only implementation for setup. Tenant records themselves are platform-level; they have no TenantId.

SchoolErpDbContext contains only Tenant, School and Campus. EF configurations use snake_case tables and columns; explicitly declared indexes/checks/alternate keys use snake_case names, while EF-generated primary/foreign key identifiers and __EFMigrationsHistory retain EF conventions. UTC timestamps map to timestamptz. The audit interceptor handles both save paths, preserves creation fields on updates and leaves user attribution for later authentication integration.

Tenant slug is unique and lowercase URL-friendly. School code uniqueness is scoped to tenant; campus code uniqueness to school. Required names/codes have nonblank checks. Parent FKs use Restrict. Campus references School with (TenantId, SchoolId) → (TenantId, Id), preventing cross-tenant associations independently of query filters. No generic repository is introduced.

InitialDatabaseSetup is generated through EF CLI in Infrastructure. Development-only startup migration and optional demo tenant seeding are controlled by Database options. Seeding is off by default. Production never applies startup migrations or seed data and must use a controlled migration deployment.

Health checks API execution and PostgreSQL connectivity; failures expose only aggregate status. Swagger is development-only; database exceptions produce safe ProblemDetails and are logged server-side. Sensitive EF logging is disabled. Production configuration must supply a connection string, allowed hosts, explicit CORS origins and secrets through deployment configuration.

ApplicationUser still prepares Identity without registering stores or creating Identity tables. JWT is not enabled. CQRS handlers can be introduced within features without a mandatory mediator.
