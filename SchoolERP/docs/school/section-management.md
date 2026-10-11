# Section management

`ClassSection` represents a section inside one class for one academic year. It is deliberately separate from subjects, enrollment, teachers, attendance, timetables, exams, fees, and rooms.

```text
Tenant
  └─ School
       └─ Academic Year
            └─ Class / Grade
                 ├─ Section A
                 ├─ Section B
                 └─ Section C
```

The API derives tenant, school, and academic-year ownership from the route's parent class. Composite restricted foreign keys enforce the entire hierarchy. Cross-tenant, cross-school, and nested cross-class lookups return not found.

Names are trimmed and unique per class using normalized application checks. Codes are uppercase, accept letters, digits, hyphens and underscores, and have a database-enforced `(school_class_id, code)` unique scope. Display order is explicit and starts at zero. Optional capacity is metadata between 1 and 500; it does not enforce enrollment. Room number is free-form metadata and is not a room entity.

Sections follow `Draft → Active → Inactive`, with reactivation supported. Draft or inactive sections may be archived; active sections must first be deactivated. Archived sections are read-only. Creation requires a Draft or Active class and Draft or Active academic year. Activation requires both the class and academic year to be Active. Suspended or archived schools block structure mutations.

API routes are `GET /api/v1/sections`, `GET /api/v1/sections/{id}`, `GET|POST /api/v1/classes/{classId}/sections`, `PUT /api/v1/sections/{id}`, and the `/change-code`, `/activate`, `/deactivate`, and `/archive` actions. Permissions are `Section.View`, `Section.Create`, `Section.Edit`, `Section.Activate`, `Section.Deactivate`, and `Section.Archive`.

Frontend routes are `/sections`, `/classes/{classId}/sections/new`, `/sections/{id}`, and `/sections/{id}/edit`. Future student, class-teacher, attendance, timetable, and exam relationships will reference `ClassSection`; they are not part of this phase.
