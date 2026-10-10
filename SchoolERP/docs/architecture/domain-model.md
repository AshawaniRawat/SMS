# Domain entity foundation

## Entity hierarchy

The domain uses one shallow hierarchy:

```text
Entity
└── AuditableEntity
    ├── Tenant
    ├── School
    └── Campus
```

`Entity` owns identity, equality, and domain-event storage. `AuditableEntity` adds `CreatedAt`, `UpdatedAt`, `CreatedBy`, and `UpdatedBy`. Tenant ownership and soft deletion remain explicit interfaces rather than universal base-class properties.

Tenant and School are current aggregate roots. `IAggregateRoot` is only a marker; it has no persistence behavior.

## Identity and equality

New entities receive a .NET `Guid.CreateVersion7()` identifier. UUID v7 keeps GUID uniqueness while improving PostgreSQL index locality compared with random UUID v4. EF Core may replace the constructor value when materializing an existing row.

Entities compare equal only when they have the same concrete type and the same non-empty identifier. Two transient entities with `Guid.Empty` do not compare equal unless they are the same object.

## Auditing and soft deletion

Audit properties contain no clock logic. Infrastructure interceptors populate UTC timestamps through `TimeProvider`. `CreatedBy` and `UpdatedBy` remain nullable for background/system operations until authenticated current-user infrastructure exists.

Soft deletion is opt-in through `ISoftDeletableEntity`; it is not part of `Entity` or `AuditableEntity`. Tenant, School, and Campus currently opt in.

## Tenant ownership

`ITenantEntity` is explicit. School and Campus are tenant-owned; Tenant is a platform entity and therefore does not implement it. The persistence tenant guard assigns an empty tenant ID only when a validated tenant scope exists and rejects mismatches.

## Domain events

Entities expose domain events as a read-only collection and provide add, remove, and clear operations. Events derive from `DomainEvent` and receive an UTC occurrence timestamp. EF configurations ignore the collection. P1-04 does not dispatch events; dispatch belongs to a later application/infrastructure task.

```csharp
school.AddDomainEvent(new SchoolChangedEvent());
```

## Validation and normalization

Domain entities enforce immediate invariants and throw `DomainException`. Application validation may provide user-friendly request validation, while database constraints remain the final persistence safeguard.

- Names are trimmed and required.
- Tenant slugs are trimmed, invariant-lowercase, and URL-safe.
- School and Campus codes are trimmed and invariant-uppercase.

The Domain project has no EF Core, Npgsql, ASP.NET Core, HTTP, or service-locator dependency. Database mappings remain in Infrastructure.
