# House management (P1-32)

Houses are persistent school-wide organizational structures. Each House belongs to one Tenant and School; it has no Campus or Academic Year key because house systems normally continue across sessions.

## Model and validation

`House` is an auditable `ITenantEntity` aggregate. Name is required and case-insensitively unique within a school. Code is trimmed, uppercased, accepts letters, digits, hyphens and underscores, and is protected by `UNIQUE(SchoolId, Code)`. Short name is optional. Display order is non-negative.

`ColorHex` is required, normalized to uppercase, and accepts only `#RRGGBB`. Arbitrary CSS, shorthand, alpha colors, functions, and invalid hex digits are rejected. Color is not unique. The UI displays both a swatch and textual name/code/hex so color is never the sole identifier.

Motto is optional up to 250 characters; description is optional up to 1,000 characters.

## Lifecycle

- Draft → Active or Archived
- Active → Inactive
- Inactive → Active or Archived
- Archived is terminal and read-only

Draft and Active schools permit creation/editing; Suspended and Archived schools block mutations. Activation specifically requires an Active school. There is no delete endpoint.

## Security, permissions, and APIs

The automatic tenant filter and explicit current-school predicate hide cross-tenant and cross-school records as nonexistent. Permissions are `House.View`, `House.Create`, `House.Edit`, `House.Activate`, `House.Deactivate`, and `House.Archive`.

Endpoints: `GET/POST /api/v1/houses`, `GET/PUT /api/v1/houses/{id}`, and POST actions `change-code`, `activate`, `deactivate`, and `archive`. Lists support status/search filters, pagination, and allow-listed sorting. Frontend routes mirror these at `/houses`, `/houses/new`, `/houses/{id}`, and `/houses/{id}/edit`.

```text
Tenant
└── School
    └── Houses
        ├── Red House
        ├── Blue House
        ├── Green House
        └── Yellow House

House (future)
├── Students
├── House Master
├── Captain / Vice Captain
└── Competition Points / Awards
```

Student membership, teachers, leaders, competitions, points, awards, allocation algorithms, attendance, and fees are deliberately deferred. Whether future student membership is academic-year-aware will be decided with the Student module.
