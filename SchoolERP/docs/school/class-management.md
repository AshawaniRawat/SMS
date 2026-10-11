# School class / grade management

School classes are academic-year structure records, not student sections. Every record is owned by one tenant, one school, and one academic year. The database enforces that the referenced school and academic year belong to that same ownership chain.

```text
Tenant
  └─ School
       └─ Academic Year
            └─ School Class / Grade
```

## Lifecycle

Classes begin in `Draft`, can become `Active`, can be `Inactive`, and may finally be `Archived`. An active class must be deactivated before archiving. Archived records are read-only. Mutations are rejected while the parent school is suspended or archived, or while the academic year is closed or archived.

Class codes are normalized to uppercase and unique within a school and academic year. `DisplayOrder` controls the default list order and accepts zero or greater.

## API

The `/api/v1/classes` resource supports scoped list, detail, create, update, and code-change operations. Lifecycle actions are `/activate`, `/deactivate`, and `/archive`. List filters include academic year, status, education level, and search text.

All ownership is derived from the authenticated tenant and current-school context. Requests cannot supply tenant or school identifiers. Cross-school records therefore behave as not found.

## Authorization

The module uses `Class.View`, `Class.Create`, `Class.Edit`, `Class.Activate`, `Class.Deactivate`, and `Class.Archive`. The React routes and individual lifecycle controls apply the corresponding permissions, while the API remains the authoritative enforcement boundary.
