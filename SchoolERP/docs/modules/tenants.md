# Tenants

A Tenant is one SchoolERP SaaS customer/account boundary. It is platform-scoped, is an aggregate root, and does not implement `ITenantEntity`. Future Platform Super Admin authorization will protect its APIs; no request header or query parameter grants platform access.

Tenant is not the same concept as School. `ABC Education Group` can be one Tenant containing `ABC Public School Delhi`, `ABC Public School Noida`, and `ABC Public School Lucknow`. A small customer may use `Sunrise School` for both its Tenant and its single School.

```text
Platform
   |
Tenant
   |
   +-- School A
   |     +-- Campus A1
   |
   +-- School B
         +-- Campus B1
```

## Lifecycle

Tenant status is stored as an integer enum:

- `Pending (1)` → Active or Archived
- `Active (2)` → Suspended or Archived
- `Suspended (3)` → Active or Archived
- `Archived (4)` → no normal transition

Activation, suspension, reactivation, and archival are domain operations. Safe repeated operations are idempotent. Archival is the terminal, non-operational state and replaces Tenant soft deletion. It does not delete Schools or Campuses.

Lifecycle timestamps and events use UTC. `ActivatedAt` records first activation, `SuspendedAt` is cleared on reactivation, and `ArchivedAt` records archival.

## Slugs and profiles

Slugs are trimmed, invariant-lowercase, at most 100 characters, and contain lowercase ASCII letters/digits separated by single hyphens. They are protected by a unique database index. Reserved values are: `admin`, `api`, `app`, `www`, `support`, `login`, `signup`, `register`, `system`, `platform`, `superadmin`, and `health`.

Email is trimmed/lowercase. Country code uses two uppercase characters. Default currency uses three uppercase characters. Timestamps remain UTC; `TimeZoneId` only records the display/configuration preference.

## API and tenant resolution

`/api/v1/tenants` supports creation, paginated search/status listing, ID lookup, slug lookup, and lifecycle operations. Slug lookup prepares later subdomain-based tenant resolution. These endpoints are marked for the future `PlatformSuperAdmin` authorization policy.

Subscriptions, plans, billing, custom domains, white labeling, quotas, feature flags, invitations, deletion, and onboarding workflows are intentionally outside P1-05.
