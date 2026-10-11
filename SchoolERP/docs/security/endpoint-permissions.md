# Endpoint permission matrix

| Endpoint group/action | Permission |
|---|---|
| Tenants GET | `Tenant.View` |
| Tenants create | `Platform.ManageTenants` |
| Tenant activate/reactivate | `Tenant.Activate` |
| Tenant suspend | `Tenant.Suspend` |
| Tenant archive | `Tenant.Archive` |
| Schools GET | `School.View` |
| Schools create/update | `School.Create` / `School.Edit` |
| School activate/reactivate/suspend/archive | matching `School.*` action |
| Campuses GET | `Campus.View` |
| Campuses create/update | `Campus.Create` / `Campus.Edit` |
| Campus lifecycle/set-main | matching `Campus.*` action |
| Academic Years GET | `AcademicYear.View` |
| Academic Years create/update | `AcademicYear.Create` / `AcademicYear.Edit` |
| Academic Year lifecycle/current/admissions | matching `AcademicYear.*` action |
| Roles GET | `Role.View` |
| Roles create/update/lifecycle | matching `Role.*` action |
| Assign/remove user role | `User.AssignRole` |
| View role permissions | `Role.View` |
| Mutate role permissions | `Role.ManagePermissions` |
| Permission catalog GET | `Permission.View` |
| Create platform/tenant user | `User.Create` |

Login, refresh, password recovery, and email verification remain anonymous. `/auth/me`, logout-all, session listing, and session revocation require authentication but no business permission. Health remains public. Individual action permissions do not imply one another.
