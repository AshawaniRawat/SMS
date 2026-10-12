# School holiday and academic calendar foundation (P1-33)

Calendar events belong to a Tenant and School. They may optionally target one Campus and/or one Academic Year. A null campus means school-wide; a null academic year means the event persists independently of academic sessions. Composite foreign keys enforce tenant and school consistency.

Events cover public and school holidays, vacations, working-day overrides, academic/administrative events, parent meetings, school events, training, and other events. Dates use `DateOnly`. All-day events store no times; timed events are limited to one date and require an end time after the start time.

Optional codes are uppercased and unique across the school. Optional colors accept only `#RRGGBB`. Concurrent events are allowed because distinct school activities can legitimately overlap. Recurrence is intentionally limited to none or annual with an optional end date; occurrences are not materialized.

Lifecycle: Draft → Published or Archived; Published → Cancelled; Cancelled → Published or Archived. Archived events are read-only. Publishing requires an Active school.

Campus-scoped users see school-wide events plus their campus events. Tenant, school, campus, and academic-year mismatches are hidden. Permissions are `Calendar.View`, `Calendar.Create`, `Calendar.Edit`, `Calendar.Publish`, `Calendar.Cancel`, and `Calendar.Archive`.

API routes are under `/api/v1/calendar-events`; frontend routes are `/calendar`, `/calendar/new`, `/calendar/{id}`, and `/calendar/{id}/edit`. The frontend provides calendar cards, list view, filtering, all-day/timed forms, working-day override, color, and lifecycle actions.

Attendance processing, timetable generation, exam/homework scheduling, notifications, recurrence expansion, and enterprise calendar synchronization are deferred.
