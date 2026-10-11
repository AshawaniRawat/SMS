# Subject management

Subjects are school- and academic-year-owned master records. They are recreated or copied for future academic years rather than being global resources.

```text
Tenant → School → Academic Year
                     ├─ Subjects
                     └─ School Classes
                           └─ ClassSubject → Subject
```

`Subject` stores its name, uppercase code, type, optional category, display order, optional credit hours and weekly-period metadata, lifecycle, and audit data. `IsElective` is derived from `SubjectType.Elective`; it is not duplicated in storage. Codes are unique for a school and academic year. Normalized names are also checked within that scope.

Credit hours must be greater than zero and at most 100. Weekly periods must be between 1 and 50. Neither value generates a timetable. Subjects use Draft, Active, Inactive, and Archived states. Archived subjects are read-only, and subjects with class mappings cannot be archived. Subject mutations require an operational school and a Draft or Active academic year.

`ClassSubject` is the class curriculum applicability mapping. It stores mandatory/optional state and class-specific display order. Its composite foreign keys require the class and subject to share the same tenant, school, and academic year. A class-subject pair is unique. Bulk replacement validates every requested subject before adding, updating, or removing mappings and saves the result atomically. During a Draft year, Draft or Active subjects are assignable; during an Active year, only Active subjects are newly assignable. Sections inherit their class curriculum; `SectionSubject` is intentionally not persisted.

APIs are available under `/api/v1/subjects` and `/api/v1/classes/{classId}/subjects`. Permissions are `Subject.View`, `Subject.Create`, `Subject.Edit`, `Subject.Activate`, `Subject.Deactivate`, `Subject.Archive`, and `Subject.ManageClassMapping`. Frontend routes are `/subjects`, `/subjects/new`, `/subjects/{id}`, `/subjects/{id}/edit`, and `/classes/{classId}/subjects`.

Teacher assignment, timetable generation, exams, attendance, student elective selection, lesson plans, and curriculum content are future work.
