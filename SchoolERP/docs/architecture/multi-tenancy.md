# Multi-tenancy architecture

`Tenant` is the SaaS customer boundary. `School`, `Campus`, `AcademicYear`, and every future `ITenantEntity` are tenant-owned. The `Tenant` entity itself is global and is not filtered.

```text
JWT
 |
 +-- user_id
 +-- tenant_id
 |
 v
ICurrentUser
 |
 v
ICurrentTenant
 |
 +--> EF Query Filter
 |
 +--> Write Guard
 |
 +--> Application Scope Validation
 |
 v
Tenant-Owned Data
```

## Trusted context

For authenticated tenant users, `ICurrentTenant` reads the canonical `tenant_id` value exposed by `ICurrentUser`. A malformed, empty, missing, or anonymous claim produces no tenant context and never broadens access. Request headers, route values, query strings, and body fields cannot change this context.

`IsPlatformScope` is independent from a missing tenant claim. It is true only when the scoped privileged `IPlatformDataAccess` abstraction is explicitly enabled by trusted infrastructure. The normal web registration always uses the restricted implementation. Platform identity, a null tenant, or a role name alone does not enable it.

## Read isolation

`SchoolErpDbContext` discovers every concrete `ITenantEntity` and adds one parameterized per-context global filter. Without explicit platform scope, a row is visible only when a current tenant exists and its `TenantId` matches. Missing context returns zero rows. Tenant and soft-delete expressions are composed with AND, and `AsNoTracking` retains the filters.

## Write isolation

`TenantGuardInterceptor` checks Added, Modified, and Deleted entries:

- Added entities with an empty tenant receive the authenticated tenant ID.
- A supplied tenant must match the authenticated tenant.
- Modified and deleted entities must belong to the authenticated tenant.
- Tenant ownership cannot change after creation.
- Missing tenant context is rejected.
- Explicit platform scope never guesses an empty target tenant and still rejects TenantId mutation.

Campus and AcademicYear composite relationships additionally ensure their tenant matches their parent School.

## API inputs and special cases

School creation currently retains `TenantId` in its compatibility DTO, but the service compares it to trusted context and rejects mismatches. Campus and AcademicYear creation derive tenant ownership from the filtered parent School. Tenant-routed role and user operations validate the route tenant against the authenticated tenant and hide mismatches.

Identity users and roles intentionally do not implement `ITenantEntity`: login and platform-aware identity workflows require explicit global lookup followed by scope validation. Permissions are global. RolePermission scope derives from its Role. Refresh tokens use explicit user/session ownership rules.

Background jobs have no HTTP claims and must create an explicit tenant execution scope before accessing tenant-owned entities. They must never default to all tenants. Migrations are schema-level operations and seeders operate only on global/platform definitions unless a target tenant is explicitly supplied.

## Query escape review

There are no tenant override headers, tenant-sensitive raw SQL queries, or bulk updates against `ITenantEntity` in the application. Approved `IgnoreQueryFilters` calls are limited to identity registration, login, refresh, and email-verification organization checks after a user/target tenant has been explicitly resolved. Refresh-token SQL and session bulk updates operate on identity/session records, not `ITenantEntity` tables.
