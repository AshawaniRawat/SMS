# Audit action catalog

Stable identifiers are defined in `AuditActions`; callers must not invent free-form variants.

- Authentication: `Authentication.LoginSucceeded`, `Authentication.Logout`
- Identity: `User.Created`, `User.Updated`, `Role.Created`, `Role.Updated`
- Tenant: `Tenant.Created`, `Tenant.Updated`, `Tenant.StatusChanged`
- School: `School.Created`, `School.ProfileUpdated`, `School.StatusChanged`
- Campus: `Campus.Created`, `Campus.Updated`, `Campus.SetMain`, `Campus.StatusChanged`
- Academic: `AcademicYear.Created`, `AcademicYear.Updated`, `AcademicYear.SetCurrent`, `AcademicYear.StatusChanged`, plus curated class, section, subject, department, house, and calendar change actions
- Security: `Role.PermissionAssigned`, `Role.PermissionRemoved`

Categories are short stable business areas such as Authentication, Identity, Tenant, School, Campus, Academic, Security, Configuration, and System. Outcomes are `Succeeded`, `Failed`, or `Denied`; routine validation failures and reads are not audited.
