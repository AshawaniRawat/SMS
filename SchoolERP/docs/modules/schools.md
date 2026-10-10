# Schools

## Purpose and ownership

A tenant is the SaaS customer or education group; a school is an operational educational institution owned by exactly one tenant. A school cannot be moved to another tenant through normal domain or API operations.

```text
Tenant
  +-- School A
  |     +-- Campus A1
  |     +-- Campus A2
  +-- School B
        +-- Campus B1
```

P1-06 only prepares the existing School-to-Campus relationship. Campus management remains out of scope.

## Profile

Required fields are `TenantId`, `Name`, `Code`, and `Status`. The optional profile includes short name, registration and affiliation references, board information, contact details, simple address fields, logo URL, timezone, default currency, and principal name. Timestamps are UTC. A future application layer may inherit timezone and currency from the tenant when school values are absent; P1-06 does not implement that fallback.

`SchoolBoardType` supports CBSE, ICSE, StateBoard, IB, Cambridge, and Other. It is intentionally generic and stored as an integer.

## Code and lifecycle

School codes are trimmed, uppercase, at most 50 characters, and contain letters or digits separated by optional hyphens or underscores. The database uniqueness scope is `(TenantId, Code)`, so separate tenants can use the same code.

Schools begin in `Draft`. Allowed transitions are:

- Draft to Active or Archived
- Active to Suspended or Archived
- Suspended to Active or Archived
- Archived has no normal outgoing transition

Activation, suspension, reactivation, and archival record UTC lifecycle timestamps and raise in-process domain events. Archival is not a hard delete.

## Application and API

Tenant-scoped endpoints are exposed at `/api/v1/schools` for create, paginated list, lookup by ID or code, profile update, activate, suspend, reactivate, and archive. There is no delete endpoint. Creating or changing schools is rejected for suspended or archived tenants; pending tenants may create schools for onboarding.

Authorization policies are placeholders until the permission module is implemented. The expected future permissions are `School.View`, `School.Create`, `School.Edit`, `School.Activate`, `School.Suspend`, and `School.Archive`.

## Security and persistence

School implements `ITenantEntity`, so the shared EF query filter hides other tenants and returns no rows when tenant context is missing. The write interceptor auto-assigns an empty tenant ID, permits an explicit matching ID, and rejects mismatched or changed ownership. The tenant foreign key uses restrictive deletion. Database constraints enforce required fields, status range, and tenant-scoped code uniqueness; unique-key races are translated to a safe conflict response.
