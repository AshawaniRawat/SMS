# Permission catalog

P1-18 introduces a global, platform-managed catalog of security capabilities. A permission describes a stable resource/module and action, such as `Student.View` or `Fee.Refund`; it is not a UI screen or button name.

## Model

`Permission` is an auditable entity with `Id`, `Code`, `Name`, `Module`, `Action`, optional `Description`, `Scope`, `IsSystemPermission`, `IsActive`, `SortOrder`, optional `Group`, and the standard creation/update audit fields. It deliberately has no `TenantId`: definitions are identical across tenants. Tenant access will be expressed by role-permission relationships in P1-19.

Codes use exactly `<PascalCaseModule>.<PascalCaseAction>`, are globally unique, case-sensitive application identifiers, and cannot be changed after creation. Metadata can only be synchronized through a catalog definition. Removed or deprecated definitions are not automatically deleted; an explicit deprecation can retain the row with `IsActive = false`.

`PermissionScope` is future authorization metadata:

- `Platform`: platform administration.
- `Tenant`: tenant-wide operations.
- `School`: school-level operations.
- `Campus`: campus-level operations.
- `Self`: operations on the authenticated user's own resource, where applicable.

Scope is not authorization logic in P1-18.

## Catalog and lifecycle

`PermissionCatalog` is the single declarative source of truth. Its immutable definitions contain deterministic IDs, codes, display metadata, scopes, and ordering. Startup/bootstrap synchronization inserts missing definitions and safely updates known metadata. It preserves the code and ID of existing rows and never deletes unknown historical permissions. All seeded definitions are system permissions, and no mutation API is exposed.

Catalog validation checks unique and correctly formatted codes, `Code == Module.Action`, non-empty names, and valid ordering. Sensitive actions remain distinct—for example `Fee.Refund`, `Fee.Waive`, `Exam.Publish`, `Payroll.Approve`, `User.AssignRole`, and `Role.Archive`.

```text
Permission Catalog
   |
   +-- Student.View
   +-- Student.Create
   +-- Fee.Collect
   +-- Fee.Refund
   +-- Exam.Publish
   +-- User.AssignRole
   +-- Audit.View
```

## Read API

All endpoints require an authenticated user while explicit permission policies are deferred to P1-20.

- `GET /api/v1/permissions` supports pagination and `module`, `scope`, `isActive`, and `search` filters. Search covers code, name, and description.
- `GET /api/v1/permissions/by-code?code=Fee.Refund` performs an exact code lookup.
- `GET /api/v1/permissions/modules` returns distinct modules and counts, with an optional `isActive` filter.

There are no POST, PUT, PATCH, or DELETE permission endpoints.

## Deferred authorization work

P1-19 will add mappings without changing permission identity:

```text
Role
  |
  +-- many Permissions
```

P1-20 will calculate and enforce permissions:

```text
Authenticated User
  |
  +-- Roles
        |
        +-- Permissions
              |
              +-- Authorization Policy
```

P1-18 does not create role mappings, permission claims, dynamic policy providers, permission attributes, or UI visibility rules.
