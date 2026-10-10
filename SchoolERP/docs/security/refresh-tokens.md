# Refresh tokens (P1-13)

Successful password login now returns a short-lived JWT access token and a rotating opaque refresh token. The raw refresh token is returned once; PostgreSQL stores only its SHA-256 hash. `POST /api/v1/auth/refresh` accepts `{ "refreshToken": "..." }` and returns a completely new access/refresh pair.

Refresh tokens expire after `Authentication:RefreshTokenLifetimeDays` (7 by default). Each login creates a distinct session. Rotation is transactional and locks the current token row, so concurrent use can produce at most one successful rotation. Reusing a consumed token is treated as likely theft and revokes every refresh token in that session. Account status, organization hierarchy, email-confirmation policy, and the Identity security stamp are checked again on refresh.

The table records session and rotation lineage, revocation/use timestamps, tenant/school/campus snapshots, audit metadata, and bounded client IP/user-agent context. Raw tokens and token hashes must never be logged. Refresh requests are rate limited independently. Access and refresh tokens must be transported only over HTTPS outside local development.

`Authentication:RefreshTokenRetentionDays` reserves the retention period for a future cleanup job. P1-13 does not add logout/revocation endpoints or a background cleanup worker; logout is scheduled separately.

```text
Login -> Access token (15 min)
      -> Refresh token A (7 days) -> token B -> token C

Replay token A -> detect reuse -> revoke the session
```

For browser production deployments, keep the access token in memory and prefer a `Secure`, `HttpOnly`, appropriately `SameSite` cookie for the refresh token. Body transport is retained for this API-development phase. Persisting refresh tokens in `localStorage` is not the preferred design because JavaScript can read them after an XSS compromise. A future cookie mode must pair its SameSite policy with the deployment's CSRF strategy.

P1-14 can call the existing single-token and session revocation service methods for logout. Future password reset/change should rotate the Identity security stamp and revoke refresh sessions. Existing access tokens remain stateless and may live until their short expiry after an account or organization is suspended.
