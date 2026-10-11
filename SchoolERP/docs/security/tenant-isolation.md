# Tenant-isolation security

Tenant isolation uses independent defenses: trusted JWT context, permission policies, route/body validation, global EF filters, write interception, and parent-child database invariants.

| Threat | Protection |
|---|---|
| Tenant A requests a Tenant B route | Tenant-routed controllers compare the route to the authenticated tenant and return not found. |
| Tenant A supplies Tenant B in a request body | Services reject the mismatch; tenant ownership is never selected from untrusted input. |
| Tenant A sends `X-Tenant-Id: B` | No production component reads this header; JWT `tenant_id` remains authoritative. |
| Tenant A queries a Tenant B entity ID | The global query filter removes the row, producing null/not found. |
| Tenant A attaches a foreign entity for update/delete | The save interceptor checks Added, Modified, and Deleted entries and throws a safe tenant mismatch. |
| Code changes an existing TenantId | Domain immutability and EF original/current-state validation reject it. |
| Tenant claim is missing or malformed | Tenant context is unavailable; reads return zero rows and writes fail. |
| Platform user has no tenant claim | No bypass is inferred. Explicit privileged platform scope is required. |

Cross-tenant write violations are warning-level security events containing operation and entity metadata, without request bodies or tokens. API errors avoid exposing foreign tenant data.

`IgnoreQueryFilters` is prohibited in tenant-facing business queries. The reviewed identity exceptions validate an already resolved user or explicit target hierarchy and are documented in the architecture guide. `ExecuteUpdate` is used only for owned refresh-token sessions; raw SQL is limited to refresh-token locking and advisory transaction locks.
