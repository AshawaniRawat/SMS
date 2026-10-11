# Roles (P1-17)

P1-17 establishes role storage, lifecycle, tenant visibility, and user assignment. It deliberately does not introduce permissions, role-permission mappings, permission claims, or permission policies.

```text
Platform
  |
  +-- PLATFORM_SUPER_ADMIN

Tenant A
  |
  +-- SCHOOL_ADMIN (global system role)
  +-- TEACHER (global system role)
  +-- Custom Role: FEE_ASSISTANT

Tenant B
  |
  +-- SCHOOL_ADMIN (global system role)
  +-- Custom Role: FEE_ASSISTANT
```

## Model and identity compatibility

The existing `ApplicationRole : IdentityRole<Guid>` and `roles` table are extended; no competing role entity exists. Roles contain tenant, code, display name, description, scope type, system/active flags, lifecycle status, sort order, and audit metadata.

Identity `Name` remains globally unique because `RoleManager`, `UserManager.AddToRoleAsync`, JWT role loading, and the default stores resolve roles by normalized name. System roles use `Name = Code`. A custom tenant role receives a deterministic internal name:

```text
TENANT_{tenantId:N}_{CODE}
```

The API exposes `Code`, not the internal name. Code is uppercase `A-Z`, digits, and underscores, up to 100 characters. A tenant may reuse a code used by another tenant, while `(TenantId, Code)` is unique inside one tenant. Database constraints preserve global normalized-name uniqueness and tenant-local code uniqueness.

`ApplicationRole` intentionally does not implement `ITenantEntity`; a global query filter would incorrectly hide reusable system roles. Role services use explicit `TenantId == currentTenant` or eligible global-role predicates.

## Types and lifecycle

`RoleType` describes assignment intent:

- `Platform`: only platform-scoped users.
- `Tenant`: tenant-scoped users.
- `School`: users with a matching tenant and a school assignment.
- `Campus`: users with a matching tenant and a campus assignment.

Custom roles start Active. They may transition Active → Inactive → Active, or Active/Inactive → Archived. Archived is terminal. Deactivation may retain existing assignments, but inactive roles cannot be newly assigned. A role with assigned users cannot be archived.

System roles are immutable through tenant operations. Tenant APIs cannot update, deactivate, or archive them. There is no hard-delete endpoint.

## Visibility and assignment

Tenant lists contain the tenant's custom roles and applicable non-platform global system roles. Cross-tenant custom roles return `404`. Assignment APIs use `RoleId`, load the role through a scoped query, validate user tenant and `RoleType`, and only then call Identity with the globally unique internal name. Duplicate assignment and removal of a missing assignment are idempotent.

The tenant role endpoints currently require authentication and an exact JWT tenant-claim/route match. This is an explicit temporary boundary: P1-20 must replace it with permission policies. Platform role assignment is not exposed, so a last-super-admin removal guard is not currently required. Bootstrap super-admin credentials and assignment remain an environment-driven future deployment task; no password is stored in source.

## System roles

The migration seeds 27 deterministic, immutable roles including `PLATFORM_SUPER_ADMIN`, `TENANT_OWNER`, `SCHOOL_ADMIN`, `PRINCIPAL`, `TEACHER`, `ACCOUNTANT`, `PARENT`, `STUDENT`, and `AUDITOR`. Deterministic IDs prepare future role-permission mappings. No permissions are seeded.

## APIs

All paths begin with `/api/v1/tenants/{tenantId}/roles` and require Bearer authentication:

- `POST /` creates a tenant custom role.
- `GET /` lists paginated visible roles.
- `GET /{roleId}` gets one visible role.
- `PUT /{roleId}` updates custom display metadata.
- `POST /{roleId}/activate`, `/deactivate`, or `/archive` changes lifecycle state.
- `POST /{roleId}/users/{userId}` assigns a role.
- `DELETE /{roleId}/users/{userId}` removes an assignment.
- `GET /users/{userId}` lists a user's assigned roles.

Responses expose no permissions or internal Identity role name.
