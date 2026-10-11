# School Admin dashboard

`GET /api/v1/dashboard` and `/dashboard` provide a tenant- and school-scoped operational overview using only implemented SchoolERP modules. Access requires `School.ViewDashboard`, granted by default to Tenant Owner, School Admin, and Principal—not Teacher.

The backend derives TenantId exclusively from authenticated context. A user's SchoolId is authoritative. A tenant-level user is automatically resolved only when exactly one non-archived school exists; zero schools return 404 and multiple schools return 409 `School selection required`. No header, route, query, or body can select another school.

Metrics include campus lifecycle totals and main campus, active academic years/current year, explicitly school/campus-scoped Identity user status totals, tenant custom plus applicable non-platform system roles, and setup health. User counts exclude unrelated tenant-level and other-school users. Recent activity is empty/omitted because no queryable audit-log module exists.

Setup completion gives equal weight to six checks: school name/code, any campus, main campus, current academic year, active School Admin/Tenant Owner assignment, and any active applicable role. Attention items cover non-active school status, missing main campus/current year/admin, unconfirmed users, and suspended campuses.

Queries are sequential, bounded, read-only, and retain tenant global filters. The UI contains real API-backed cards, academic-year and campus/user overviews, setup progress, attention, skeleton, retry, access-denied, and multi-school states. Student, teacher, fee, attendance, exam, and platform metrics are intentionally absent.
