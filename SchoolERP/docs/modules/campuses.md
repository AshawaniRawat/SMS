# Campuses

A campus is a physical or operational location within one school. It belongs permanently to both that school and its tenant.

```text
Tenant
  +-- School
        +-- Main Campus
        +-- Secondary Campus
```

Campus codes are trimmed, uppercase, and contain letters or digits with optional hyphen or underscore separators. They are unique within a school, not globally. The database enforces `(school_id, code)` uniqueness.

## Main campus

The first campus created for a school automatically becomes its main campus unless the request explicitly specifies otherwise. Setting another campus as main atomically removes the previous designation. A PostgreSQL partial unique index on `school_id`, filtered to non-deleted main campuses, guarantees at most one main campus per school. A school may temporarily have no main campus during setup.

## Lifecycle and parent rules

Campuses begin in `Draft`. Supported transitions are Draft to Active or Archived, Active to Suspended or Archived, and Suspended to Active or Archived. Archived campuses cannot be restored through normal operations.

Campus creation is allowed for Draft or Active schools and Pending or Active tenants. Creation is blocked for suspended or archived parents. Activation and reactivation require an Active school and an operational tenant. Other write operations are blocked for suspended or archived tenants.

## Profile and location

The profile contains contact information, a simple address, timezone, and optional decimal coordinates. Latitude and longitude use PostgreSQL `numeric(9,6)` and are constrained to valid geographic ranges. A missing campus timezone may later fall back to school and tenant configuration; that resolution is not part of P1-07.

## Security and relationships

`Campus` implements `ITenantEntity`. Global query filters hide campuses from other tenants and return no campus rows without tenant context. The write interceptor rejects cross-tenant changes. A composite foreign key `(tenant_id, school_id)` to School prevents a campus from referencing another tenant's school. Tenant and School deletion both use restrictive foreign keys; there is no hard-delete API.

The API exposes tenant-scoped create, paginated list, profile and code updates, main-campus switching, lookup, and lifecycle operations. Permission policy placeholders cover future `Campus.View`, `Campus.Create`, `Campus.Edit`, `Campus.Activate`, `Campus.Suspend`, `Campus.Archive`, and `Campus.SetMain` policies. Academic relationships are intentionally deferred.
