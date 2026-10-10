# Academic years

An academic year is a school-level academic session shared by that school's campuses. It belongs permanently to one tenant and one school.

```text
Tenant
  +-- School
        +-- Academic Year 2026-27 [Current]
        +-- Academic Year 2027-28
```

Future modules will use it as their scope:

```text
Academic Year
  +-- Classes and Sections
  +-- Enrollment and Attendance
  +-- Fees and Exams
  +-- Timetable
```

Those downstream modules are not implemented in P1-08.

## Identity, dates, and overlap

Names are flexible and codes are trimmed, uppercase, and unique within a school. Codes may be reused by different schools. Start and end values use PostgreSQL `date` through .NET `DateOnly`. The end must be after the start and a session may not exceed 24 months.

Academic-year ranges may not overlap within the same school, including archived historical sessions. Adjacent ranges are allowed. The application uses `existing.StartDate <= new.EndDate && existing.EndDate >= new.StartDate` for overlap detection.

## Lifecycle

Academic years begin in Draft. Allowed transitions are Draft to Active or Archived, Active to Closed, and Closed to Archived. Active years cannot be archived directly. Closing or archiving clears the current flag and closes admissions.

Creation is allowed while a school is Draft or Active, but not Suspended or Archived. Activation requires both an Active tenant and Active school.

## Current academic year

Only an Active year may become current. Setting a new current year unsets the previous current year in the same persistence transaction. A PostgreSQL partial unique index on `school_id WHERE is_current = true` provides database-level race protection.

## Admissions and lock date

Admissions may be opened for Draft or Active years with an optional date window. The admission window may begin before the academic year, but its end cannot precede its start. Closing admissions retains the configured dates.

`LockDate` is an optional foundation for later transaction restrictions. It must fall between the academic year's start and end dates. P1-08 does not enforce locking in downstream modules.

## Security and persistence

AcademicYear implements `ITenantEntity`; tenant query filters and write guards apply. A composite `(tenant_id, school_id)` foreign key guarantees the school belongs to the same tenant. Tenant and School deletion are restrictive. No hard-delete endpoint exists.

The API exposes create, tenant-scoped pagination and lookup, Draft updates, activation, current-year switching, close/archive, and admission-window operations under `/api/v1/academic-years` and school-specific lookup routes. Authorization policy names are placeholders until the permission system is implemented.
