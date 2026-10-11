# Logout and sessions (P1-14)

SchoolERP logout revokes refresh-token sessions. It does not persist or blacklist JWT access tokens. A previously issued access token is stateless and can remain valid until its configured short expiry (15 minutes by default). On logout, clients must discard the access token, clear local user state, and navigate to login.

## API behavior

- `POST /api/v1/auth/logout` is anonymous so a client can log out after its access token expires. It accepts `{ "refreshToken": "..." }`, hashes the opaque token, identifies its session, and revokes the complete rotation chain. Empty input is `400`; unknown, expired, used, reused, and already-revoked tokens are otherwise idempotent `204` responses and disclose no token state.
- `POST /api/v1/auth/logout-all` requires Bearer authentication and revokes every refresh-token session owned by the JWT subject. It never accepts a user ID. Other devices retain stale client-side tokens, but their next refresh fails.
- `GET /api/v1/auth/sessions?pageNumber=1&pageSize=20&activeOnly=true` requires authentication. It returns newest sessions first, at most 100 per page, with one aggregate row per `SessionId`. It exposes dates and bounded IP/user-agent context, never raw tokens, hashes, security-stamp snapshots, or rotation IDs.
- `POST /api/v1/auth/sessions/{sessionId}/revoke` requires authentication. It returns `404` when the session is absent or belongs to another user, `204` when owned, including when it is already revoked.

`IsCurrent` is currently `false`: access JWTs deliberately remain independent of refresh-session storage and do not contain `session_id`. IP and user agent are display/audit hints, not trusted identity or device fingerprints.

```text
Login
  |
  +-- Session A
  |     +-- Refresh A1 -> A2 -> A3
  |
  +-- Session B
        +-- Refresh B1 -> B2

Logout Session A -> revoke the A chain; Session B remains active
Logout All       -> revoke the A and B chains
```

## Concurrency and audit

Refresh and every revocation path acquire the same PostgreSQL transaction-scoped advisory lock derived from `UserId`. The lock is obtained before a token row is locked or changed. Thus a refresh that wins the race is followed by revocation of its replacement, while a revocation that wins makes refresh fail. Bulk updates atomically set `RevokedAtUtc`, a centralized reason, bounded `RevokedByIp`, and increment the concurrency version.

Structured security events include `LogoutSucceeded`, `LogoutIdempotent`, `AllUserSessionsRevoked`, `SessionRevoked`, and `SessionRevocationFailed`, with user/session/IP when known. Raw refresh tokens are never logged. Logout does not check user, tenant, school, or campus operational status, so suspension cannot prevent session cleanup.

## Access-token and transport limitations

Revocation immediately prevents future refresh; it does not immediately invalidate an access JWT already held by a client. This is the intentional tradeoff behind short access-token lifetime and avoiding a database/cache lookup on every authorized request. Immediate invalidation would require a future explicit design such as security-stamp/token-version validation or a distributed revocation cache.

P1-13 currently transports refresh tokens in JSON bodies, so there is no server cookie to clear and no cookie CSRF surface in this phase. A future browser cookie mode must use `Secure`, `HttpOnly`, an appropriate `SameSite` value and restricted path; logout must delete the cookie with identical name/domain/path settings. Cross-site cookies also require an anti-CSRF strategy.

## Example

```http
POST /api/v1/auth/login
Content-Type: application/json

{ "email": "user@example.com", "password": "example-only" }

POST /api/v1/auth/logout
Content-Type: application/json

{ "refreshToken": "<opaque refresh token returned by login>" }
```

After logout, retrying `POST /api/v1/auth/refresh` with that session's token returns `401`. Documentation examples use placeholders only; do not print tokens in logs.
