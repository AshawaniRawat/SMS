# Login validation workflow (P1-11)

P1-11 validates credentials and account eligibility but deliberately issues no access token, refresh token, cookie, or role/permission claims.

```text
Login Request
    |
    v
Global email lookup
    |
    v
Identity password check + lockout
    |
    v
Account status / IsActive
    |
    v
Email confirmation policy
    |
    v
Tenant / School / Campus hierarchy and status
    |
    v
2FA check
    |
    v
Authenticated user result

P1-12: Authenticated user result -> JWT access token
```

## Credential and lockout behavior

`POST /api/v1/auth/login` accepts only email and password. Email lookup is global because email is globally unique and `ApplicationUser` has no tenant query filter. Unknown accounts and incorrect passwords both return `401` with `Auth.InvalidCredentials` and the message `Invalid email or password.` No user or organizational data is returned.

Passwords are checked only with ASP.NET Core Identity's password hasher through `UserManager.CheckPasswordAsync`. Incorrect passwords use `AccessFailedAsync`; the configured threshold is five attempts and the default lockout is 15 minutes. Successful password validation resets a non-zero failure count. No custom password counter or comparison exists.

The endpoint has an in-process fixed-window rate limit of ten requests per minute per client IP and returns `429` when exceeded. A distributed limiter will be required when the API is deployed across multiple instances.

## Eligibility

Only `Active` users with `IsActive == true` can complete login. When `Identity:RequireConfirmedEmail` is enabled, the email must be confirmed. Platform users require null tenant, school, and campus scope. Tenant users require an Active tenant; school and campus users additionally require Active parents and an internally consistent hierarchy. Pending/Draft, Suspended, Archived, missing, or mismatched organizational records reject login with a safe `403` response.

Password validation occurs before detailed account and organization checks so callers without the password cannot inspect account state. Login outcomes are logged using user and scope IDs when known; passwords, hashes, stamps, and request objects are never logged.

Users with `TwoFactorEnabled` receive HTTP 200 with `RequiresTwoFactor = true`. P1-11 does not bypass 2FA and does not update `LastLoginAt` until that future challenge is completed. For users without 2FA, a successful login records UTC `LastLoginAt` through `UserManager.UpdateAsync`; an update failure prevents authentication success.

The returned DTO contains identity and scope summary fields only. It contains no password data, access/refresh token, reset token, security stamp, concurrency stamp, or access-failure count.
