# Role-permission mapping

P1-19 connects roles to explicit positive permission grants. `RolePermission` contains `Id`, `RoleId`, `PermissionId`, `IsSystemAssigned`, and standard audit fields. It has no `TenantId`; tenant ownership is derived from `ApplicationRole.TenantId`, avoiding duplicated scope data. Restrictive foreign keys preserve history and a unique `(RoleId, PermissionId)` constraint prevents duplicate grants.

Tenant administrators can read applicable system-role grants and manage only custom roles belonging to their tenant. System roles are catalog-managed, archived roles are read-only, and only active permissions can be newly assigned. Assigning an existing grant and removing a missing grant are idempotent.

## Scope compatibility

The centralized compatibility service applies this conservative matrix:

| Role type | Allowed permission scopes |
|---|---|
| Platform | Platform, Tenant, School, Campus, Self |
| Tenant | Tenant, School, Campus, Self |
| School | School, Campus, Self |
| Campus | Campus, Self |

Exceptions must be added explicitly to the policy rather than bypassing validation.

## Bulk replacement

`SetPermissions` normalizes duplicate IDs, validates every requested permission and the role before changing tracked mappings, calculates additions/removals, and persists the diff with one `SaveChanges` transaction. Empty input removes all grants from a mutable custom role. Unknown, inactive, or incompatible permissions fail the entire operation without partial changes.

## Effective permissions

Effective user permissions are queried in one join through `user_roles`, active roles, `role_permissions`, and active permissions. Codes are distinct and sorted. A null tenant never implies super-admin access; platform access still requires an explicitly assigned platform role.

```text
User
  |
  +-- Role A
  |     +-- Student.View
  |     +-- Attendance.View
  |
  +-- Role B
        +-- Homework.View
        +-- Student.View

Effective: Attendance.View, Homework.View, Student.View
```

## API

Authenticated same-tenant routes are:

- `GET /api/v1/tenants/{tenantId}/roles/{roleId}/permissions`
- `POST /api/v1/tenants/{tenantId}/roles/{roleId}/permissions/{permissionId}`
- `DELETE /api/v1/tenants/{tenantId}/roles/{roleId}/permissions/{permissionId}`
- `PUT /api/v1/tenants/{tenantId}/roles/{roleId}/permissions`

P1-19 does not add permission claims to JWTs, direct user permissions, deny grants, role inheritance, middleware, dynamic policies, or endpoint permission enforcement. Those authorization decisions remain P1-20 work.
