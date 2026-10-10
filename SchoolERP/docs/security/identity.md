# ASP.NET Core Identity foundation (P1-09)

## Model and boundaries

`ApplicationUser : IdentityUser<Guid>` stores credentials and account-security data only. Its custom fields are names, optional organization scope, lifecycle state, login/password timestamps, and audit fields. Teacher, employee, parent, and student business data belongs in separate future profiles linked to the user.

`ApplicationRole : IdentityRole<Guid>` establishes Guid-keyed roles now and adds description, system-role, and audit fields. Role assignment, permissions, and authorization policies are intentionally deferred.

`ApplicationUser` does **not** implement `ITenantEntity`. The standard tenant global filter would hide platform users and would break global, case-insensitive email lookup during authentication. Globally unique email is the credential identifier; tenant administration must use `GetUsersForTenantAsync`, which explicitly applies `TenantId == tenantId`. A null tenant is only a scope shape and never grants platform privileges.

## Scope rules

- Platform user: `TenantId`, `SchoolId`, and `CampusId` are null.
- Tenant user: `TenantId` is present; tenant must exist and may be Pending or Active. Creation is blocked for Suspended or Archived tenants.
- A school requires a tenant and must belong to that tenant. Suspended or Archived schools cannot receive new users.
- A campus requires a school and tenant and must belong to both. Archived campuses cannot receive new users.
- Scope changes are not part of generic profile editing. Tenant reassignment will require a dedicated, authorized workflow.
- Optional foreign keys use `RESTRICT`, so organization deletion never cascades to credentials.

## Credentials and lifecycle

Email is required, trimmed/lowercased on creation, normalized by Identity, and protected by a unique `normalized_email` database index. For the MVP, `UserName` starts as email while remaining a separate Identity field for future strategy changes.

New users are Pending unless the trusted creation caller requests immediate activation. Supported lifecycle operations are activation, suspension, lock synchronization, and deactivation. Deactivation is the long-term disable mechanism; there is no hard-delete API. Passwords are accepted only by `UserManager.CreateAsync` and are never manually hashed, logged, audited, or returned.

Default policy is configurable under `Identity`: minimum 8 characters, four unique characters, and at least one uppercase, lowercase, digit, and non-alphanumeric character. New users are lockout-enabled; five failures lock for 15 minutes. `Identity:RequireConfirmedEmail` defaults to false for local development and can be true in production. Default token providers, security stamps, concurrency stamps, phone fields, and `TwoFactorEnabled` are preserved for later confirmation, reset, session invalidation, and 2FA flows.

## Persistence

Identity uses `users`, `roles`, `user_roles`, `user_claims`, `user_logins`, `user_tokens`, and `role_claims`, with snake_case columns. Existing `tenants`, `schools`, `campuses`, and `academic_years` remain unchanged. The migration is `AddAspNetCoreIdentityFoundation`.

The application abstraction exposes safe summaries and creation/lifecycle operations. Summaries never contain password hashes, security stamps, or concurrency stamps. Identity errors are converted to safe application messages. No user administration HTTP endpoints are exposed until authorization exists.

## Roadmap separation

P1-09 provides storage and account-management foundations only. P1-10 registration, P1-11 login, P1-12 JWT access tokens, P1-13 refresh tokens, P1-14 logout, P1-15 password reset, and P1-16 email verification remain deferred.

Future profile shape:

```text
ApplicationUser
  +-- Teacher / Employee profile
  +-- Parent profile
  +-- Student login/profile mapping
```
