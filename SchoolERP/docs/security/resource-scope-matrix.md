# Resource scope matrix

| Endpoint | Resource | Permission | Tenant | School | Campus | Foreign/mismatch |
|---|---|---|---|---|---|---|
| `GET /api/v1/schools/{id}` | School | `School.View` | global filter | user org scope | n/a | 404 |
| `GET /api/v1/schools` | School collection | `School.View` | global filter | user org scope | n/a | excluded from data and count |
| `GET /api/v1/campuses/{id}` | Campus | `Campus.View` | global filter | user org scope | user org scope | 404 |
| `GET /api/v1/schools/{schoolId}/campuses` | Campus collection | `Campus.View` | global filter | parent validated | user org scope | 404 parent |
| `GET /api/v1/schools/{schoolId}/campuses/{campusId}` | Campus | `Campus.View` | global filter | combined predicate | user org scope | 404 |
| `GET /api/v1/academic-years/{id}` | AcademicYear | `AcademicYear.View` | global filter | user org scope | n/a | 404 |
| `GET /api/v1/schools/{schoolId}/academic-years/{academicYearId}` | AcademicYear | `AcademicYear.View` | global filter | combined predicate | n/a | 404 |
| `GET /api/v1/tenants/{tenantId}/roles/{roleId}` | Role | `Role.View` | explicit predicate | role semantics | role semantics | 404 |
| `GET /api/v1/tenants/{tenantId}/roles/users/{userId}` | User roles | `Role.View` | explicit user predicate | role semantics | role semantics | 404 |
| `PUT /api/v1/tenants/{tenantId}/roles/{roleId}/permissions` | RolePermission | `Role.ManagePermissions` | role resolved first | role semantics | role semantics | 404 |
| `POST /api/v1/auth/sessions/{sessionId}/revoke` | Session | authenticated user | current user | n/a | n/a | 404 |
| `GET /api/v1/dashboard` | School dashboard | `School.ViewDashboard` | trusted current tenant | resolved current school | parent school | denied/404 |
| `GET /api/v1/school/profile` | School profile | `School.View` | trusted current tenant | resolved current school | parent school | no caller-supplied ID |
| `PUT /api/v1/school/profile` | School profile | `School.Edit` | trusted current tenant | resolved current school | parent school | no caller-supplied ID; archived is 409 |
| `GET /api/v1/campuses` | Campus collection | `Campus.View` | global filter | trusted current school | own campus when campus-scoped | foreign rows excluded from data/count |
| `GET /api/v1/campuses/{id}` | Campus details | `Campus.View` | global filter | combined predicate | own campus when campus-scoped | 404 |
| `POST /api/v1/campuses` | Campus | `Campus.Create` | derived | trusted current school | campus-scoped creation denied | 403/404 |
| `PUT /api/v1/campuses/{id}` | Campus | `Campus.Edit` | global filter | combined predicate | own campus | 404; archived is 409 |
| `POST /api/v1/campuses/{id}/{lifecycle}` | Campus lifecycle/main | matching Campus permission | global filter | combined predicate | own campus | 404; invalid transition is 409 |
| `GET /api/v1/academic-years` | Academic Year collection | `AcademicYear.View` | global filter | trusted current school | parent school | foreign rows excluded from data/count |
| `GET /api/v1/academic-years/{id}` | Academic Year | `AcademicYear.View` | global filter | combined current-school predicate | parent school | 404 |
| `POST /api/v1/academic-years` | Academic Year | `AcademicYear.Create` | derived | trusted current school | parent school | no caller-supplied ownership |
| Academic Year mutation endpoints | Academic Year | matching AcademicYear permission | global filter | combined current-school predicate | parent school | 404; invalid state is 409/400 |
| `GET /api/v1/classes` | School Class collection | `Class.View` | global filter | trusted current school | n/a | foreign rows excluded from data/count |
| `GET /api/v1/classes/{id}` | School Class | `Class.View` | global filter | combined current-school predicate | n/a | 404 |
| `POST /api/v1/classes` | School Class | `Class.Create` | derived | trusted current school and scoped academic year | n/a | no caller-supplied tenant/school; invalid parent is 404/409 |
| School Class mutation endpoints | School Class | matching Class permission | global filter | combined current-school predicate | n/a | 404; invalid parent/state or duplicate code is 409 |
| `GET /api/v1/sections` | ClassSection collection | `Section.View` | global filter | trusted current school | parent class/year filters validated | foreign rows excluded from data/count |
| `GET /api/v1/sections/{id}` | ClassSection | `Section.View` | global filter | combined current-school predicate | immutable parent class | 404 |
| `GET /api/v1/classes/{classId}/sections` | ClassSection collection | `Section.View` | global filter | parent class current-school predicate | route class | foreign/mismatched class is 404 |
| `POST /api/v1/classes/{classId}/sections` | ClassSection | `Section.Create` | derived from class | derived from class | derived class/year | foreign parent is 404; invalid state/duplicate is 409 |
| ClassSection mutation endpoints | ClassSection | matching Section permission | global filter | combined current-school predicate | immutable parent class | foreign resource is 404; invalid state is 409 |

Tenant-level users have no SchoolId or CampusId claim and can operate across schools in their tenant. School-scoped users are restricted to their SchoolId. Campus-scoped users are additionally restricted to their CampusId for campus resources.
