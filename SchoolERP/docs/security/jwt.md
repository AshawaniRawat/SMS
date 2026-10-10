# JWT access tokens (P1-12)

P1-12 converts a completed password/account validation result into a short-lived, signed bearer access token. Tokens are stateless and are not persisted. Refresh tokens, rotation, revocation, logout, roles, and permissions remain outside this phase.

## Configuration and secrets

`Jwt` requires `Issuer`, `Audience`, `SigningKey`, `AccessTokenLifetimeMinutes`, and `ClockSkewSeconds`. Startup validation requires a non-placeholder signing key of at least 32 UTF-8 bytes, a 5–60 minute lifetime, and 0–300 seconds of skew. The defaults are a 15-minute lifetime and 30-second skew.

HMAC SHA-256 signs each token. Production must supply `Jwt__SigningKey` from an environment variable or managed secret store. Never commit the key or log a token/Authorization header. Generate a development key and store it with user-secrets:

```powershell
$key = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
dotnet user-secrets set "Jwt:SigningKey" $key --project src/SchoolERP.Api
```

JWTs must travel only over HTTPS outside local development.

## Claims and validation

Tokens contain `sub`, `jti`, `iat`, `email`, `given_name`, `family_name`, and a compatible name-identifier claim. Optional `tenant_id`, `school_id`, and `campus_id` claims come only from the persisted, validated user scope. Platform tokens omit all organizational claims. Optional roles use the standard role claim type, but role loading/management is deferred.

Bearer validation requires the configured issuer, audience, signature, and lifetime, plus an expiration and signed token. The API uses a small configurable clock skew. Wrong signatures, issuer, audience, malformed tokens, and expired tokens receive 401.

`ICurrentUser` safely parses authenticated claims. `ICurrentTenant` uses the authenticated `tenant_id` claim; request headers cannot override it. An absent tenant claim never grants platform authorization. Existing tenant query filters therefore isolate authenticated tenant requests, including attempts to address another tenant's school or campus.

## API usage

Successful `POST /api/v1/auth/login` responses contain `accessToken`, `accessTokenExpiresAtUtc`, `tokenType: Bearer`, and the safe user summary. A 2FA-required response contains no token. `GET /api/v1/auth/me` requires bearer authentication and returns only safe current-user claims.

Swagger exposes a Bearer input for development; it never embeds a token or signing key. Browser clients should keep short-lived access tokens in memory when practical. Rotating refresh-token persistence and long-session behavior are documented in [refresh-tokens.md](refresh-tokens.md).
