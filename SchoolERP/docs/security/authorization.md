# Permission-based authorization

P1-20 authorizes business operations through explicit database-backed grants:

```text
JWT
 |
 v
Authenticated User
 |
 v
PermissionAuthorizationHandler
 |
 v
User Roles
 |
 v
RolePermissions
 |
 v
Active Permissions
 |
 +-- contains required code? -- YES --> Allow
 |
 +-- NO -----------------------> 403
```

The JWT carries identity and tenant/school/campus session context, not the effective permission list. Permissions are resolved through active user roles, role-permission mappings, and active permission definitions. The user account must also remain active. Changes therefore apply on the next request without issuing a new token.

## Dynamic policies

`[RequirePermission(PermissionCatalog.Student.View)]` produces the policy name `Permission:Student.View`. The dynamic policy provider creates an authenticated policy with one immutable `PermissionRequirement`; permissions are not registered individually. Codes use exact ordinal matching, with no case folding, wildcard behavior, role-name shortcut, or implicit super-admin bypass. Multiple attributes are AND requirements.

The asynchronous handler uses `ICurrentUser`, never reparses claims independently, and logs safe denial fields: user ID, tenant ID, permission code, method, and path. Tokens and mappings are never logged. Routine denials use application logs rather than high-volume audit rows.

## Request caching

`UserPermissionService` is scoped. The first permission check loads an ordinal `HashSet<string>` through the P1-19 effective-permission query; later checks for that user in the same request reuse it. There is no cross-request cache or Redis, so no invalidation protocol is needed yet.

## Responses and isolation

Unauthenticated requests receive `401` with `Authorization.Unauthenticated`. Authenticated callers lacking a grant receive `403` with `Authorization.PermissionDenied`. Responses do not expose role names or mapping details.

Permission authorization answers what the caller may do. Tenant route validation, EF query filters, and application ownership checks independently constrain which data they may access. A tenant-A user with `School.Edit` still cannot edit tenant-B data.

`PLATFORM_SUPER_ADMIN` has no code bypass; it works because P1-19 explicitly maps every active catalog permission. A null tenant alone grants nothing.

`GET /api/v1/auth/me/permissions` returns the current effective codes sorted alphabetically for frontend UX. Hiding UI controls is not security—the backend policy remains authoritative.

A distributed cache may be added later only with invalidation for role assignment/status, mapping, and permission-status changes.
