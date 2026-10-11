# Department management (P1-31)

Departments are durable school structures and are not tied to an academic year. Every department is tenant- and school-owned. `CampusId = null` means school-wide; otherwise the department belongs to exactly one campus in the same tenant and school. Campus assignment is immutable.

## Model

`Department` is a `TenantAuditableEntity`-equivalent aggregate (`AuditableEntity`, `ITenantEntity`, `IAggregateRoot`). It stores a required name and school-unique normalized code, optional short name, contact details, location, description, display order, and `HeadName`. `HeadName` is display-only: no Identity or Employee foreign key is created. A later Employee module can replace or link this field.

Types are Academic, Administrative, Finance, Human Resources, Library, Transport, IT, Security, Operations, and Other. `IsAcademic()` groups the Academic type without adding a second category model.

Codes are trimmed, uppercased, limited to 50 characters, and accept only letters, digits, hyphens, and underscores. Both code and normalized name are unique across a school, regardless of campus.

## Lifecycle

- Draft → Active or Archived
- Active → Inactive
- Inactive → Active or Archived
- Archived is terminal and read-only

Activation requires an Active school and, for a campus-specific department, an Active campus. Creating or editing structure is blocked for Suspended and Archived schools. No hard-delete endpoint exists.

## Security and visibility

The global tenant query filter applies automatically. Every query also resolves the current school. Foreign tenant, school, campus, and sibling-campus resources return 404-style absence. A campus-scoped caller sees school-wide departments plus departments for their own campus; totals are calculated after that filter.

Permissions: `Department.View`, `Department.Create`, `Department.Edit`, `Department.Activate`, `Department.Deactivate`, and `Department.Archive`. Platform Super Admin, Tenant Owner, and School Admin receive all through their scope sets. Principal receives all except archive; HR Manager and Teacher receive view.

## API and UI

APIs: `GET/POST /api/v1/departments`, `GET/PUT /api/v1/departments/{id}`, and POST actions `change-code`, `activate`, `deactivate`, `archive`. List filters include campus (or school-wide), type, status, search, pagination, and sorting.

Routes: `/departments`, `/departments/new`, `/departments/{id}`, `/departments/{id}/edit`. Forms use React Hook Form and Zod; code and campus are read-only after creation.

## Architecture

```text
Tenant
└── School
    ├── School-wide Departments (Accounts, HR, Administration)
    ├── Campus A ── Departments (Mathematics, Science)
    └── Campus B ── Departments (Mathematics, IT)

Department (future)
├── Employees / Teachers
├── Head of Department
└── Payroll and HR reporting
```

Employee assignments, HOD employee links, payroll, timetable ownership, sub-departments, cost centres, and department-level permission scopes are intentionally deferred.
