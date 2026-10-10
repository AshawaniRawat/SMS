# User registration and account creation (P1-10)

P1-10 exposes two explicit administrative workflows. It does not provide anonymous self-registration.

```text
Platform user                    Tenant user
TenantId = null                  Tenant
SchoolId = null                    |
CampusId = null                    +-- optional School
                                           |
                                           +-- optional Campus
```

## Workflows and API safety

- `CreatePlatformUserAsync` always constructs a user with null tenant, school, and campus scope.
- `CreateTenantUserAsync` requires a tenant and verifies every optional school and campus against the complete hierarchy.
- Development endpoints are `POST /api/v1/platform/users` and `POST /api/v1/tenants/{tenantId}/users`.
- Both endpoints return 404 outside Development. They must be replaced with Platform Super Admin and tenant user-creation authorization policies when authorization is implemented.
- Success returns HTTP 201 and a safe user summary. No password, hash, security/concurrency stamp, access-failure count, confirmation token, role, JWT, or refresh token is returned.

Cross-tenant schools and cross-school campuses use scope-safe not-found responses. Pending/Active tenants and Draft/Active schools and campuses allow onboarding. Suspended or Archived parents reject creation. Tenant identity comes from the route, not the request body; future authorization must also require the authenticated caller's tenant to match that route.

## Credentials and account state

Email is trimmed, lowercased, used as the initial username, and then normalized by ASP.NET Core Identity. Email is globally unique; duplicate requests return `409 Conflict` with `User.EmailAlreadyExists`. Passwords are passed only to `UserManager.CreateAsync`. They are never manually hashed, persisted as plaintext, included in DTOs, or logged. Failed Identity validation does not create a user.

`Identity:ActivateNewUsersImmediately` defaults to `false`, producing `Pending` and inactive accounts. `Identity:AutoConfirmAdminCreatedUsers` defaults to `false`, so `EmailConfirmed` remains false. Both settings can be overridden by environment configuration. New accounts have Identity lockout enabled and phone confirmation disabled.

Roles and permissions are intentionally separate. Account creation does not make an account a School Admin or grant any access. Rate limiting, protected authorization policies, role assignment, login, tokens, password recovery, and confirmation delivery belong to later phases.
