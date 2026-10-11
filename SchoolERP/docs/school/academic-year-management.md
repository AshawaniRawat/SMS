# Academic Year management

P1-27 administers sessions for the authenticated current School through `/academic-years`, `/academic-years/new`, `/academic-years/{id}`, and `/academic-years/{id}/edit`.

## Scope and permissions

Normal requests never accept TenantId or SchoolId. `ICurrentSchoolResolver` selects the trusted school; ambiguous tenant-level context returns `SchoolSelectionRequired`. Every detail and mutation combines the requested ID with the resolved school, making foreign-tenant and foreign-school records indistinguishable from missing records.

`AcademicYear.View`, `Create`, `Edit`, `Activate`, `SetCurrent`, `Close`, `Archive`, and `ManageAdmissions` protect their corresponding operations. Lock-date changes reuse `AcademicYear.Edit`.

## Rules

Names are required and limited to 100 characters. Codes are normalized uppercase, limited to 50 characters, and unique per School. EndDate must be after StartDate and duration cannot exceed 24 months. Historical archived sessions continue to participate in the same-School overlap check.

New sessions are Draft and neither current nor open for admissions. Draft sessions permit full editing. Active sessions permit name, description, admission-window, and lock-date changes, while dates and code are immutable. Closed and Archived sessions are read-only.

Lifecycle is Draft → Active → Closed → Archived, with Draft → Archived also allowed. Active cannot be archived directly. Only Active sessions can become Current. Switching Current unsets the previous session in the same persistence operation, while the database partial unique index enforces at most one current session per School. Closing clears both Current and admissions-open state.

Admissions may open for Draft or Active sessions with an optional ordered date window and close idempotently. LockDate may be null or between the academic StartDate and EndDate. Future modules may interpret LockDate; P1-27 does not lock fees, attendance, exams, or enrollment.

## API

The API provides current-school list/detail/create/update, `activate`, `set-current`, `close`, `archive`, `admissions/open`, `admissions/close`, and `PUT /{id}/lock-date`. List filters include status, current, admissions-open, search, pagination, and allow-listed sorting. There is no delete endpoint.
