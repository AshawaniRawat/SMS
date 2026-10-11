# Email verification (P1-16)

SchoolERP verifies ownership of the email already registered on an account using ASP.NET Core Identity and Data Protection. It does not assign roles, grant permissions, issue authentication tokens, or implement email-address changes.

```text
Registration
   |
   v
EmailConfirmed = false
   |
   v
Generate Confirmation Token
   |
   v
Send Verification Email
   |
   v
Confirm Endpoint
   |
   v
EmailConfirmed = true
   |
   +--> optional eligible Pending -> Active
   |
   v
Normal Login
```

## Token and link

`UserManager.GenerateEmailConfirmationTokenAsync` creates a purpose-isolated Identity token. A named `EmailConfirmation` Data Protection provider gives confirmation tokens a 24-hour default lifespan without changing the one-hour password-reset lifespan. The raw token is never stored. It is Base64Url-encoded with the shared identity-token encoder and placed in:

```text
{Frontend:BaseUrl}/verify-email?userId=<user-id>&token=<encoded-token>
```

Multi-instance production deployments must share and persist Data Protection keys. The verification page should avoid third-party trackers, set a restrictive referrer policy, and remove the token from the address bar after capture.

## Confirmation

`POST /api/v1/auth/confirm-email` accepts `userId` and the encoded token. Malformed, expired, invalid, and unknown-user requests return the same safe `400` message. Identity sets `EmailConfirmed` only after token validation. Repeating confirmation after success is idempotent and returns `200`; no JWT or refresh token is returned.

Confirmation is limited to 20 attempts per 15 minutes per IP. It does not change the security stamp or revoke existing sessions.

## Resend and enumeration protection

`POST /api/v1/auth/resend-email-verification` accepts an email address and always returns `202` with:

> If an account exists and requires verification, a verification email has been sent.

Unknown, already-confirmed, and deactivated accounts receive the same response and no email. Unconfirmed pending, active, locked, or suspended accounts may receive the message. Delivery failures are logged but remain hidden behind the generic response. The endpoint is limited to five attempts per 15 minutes per IP.

Registration attempts verification delivery after successfully persisting an unconfirmed user. Mail failure does not delete the account or fail registration; resend supplies the recovery path.

## Activation policy

`Identity:ActivateUserAfterEmailConfirmation` defaults to `false`, so verification normally changes only `EmailConfirmed`. Suspended, locked, deactivated, and active statuses are always preserved.

When explicitly enabled, only a `Pending` tenant-scoped user can be activated, and only when its tenant and any assigned school/campus are all Active with a consistent hierarchy. Platform users are never auto-activated because email ownership is not proof of administrative authorization. Confirmation can still succeed when activation is ineligible; the user remains Pending.

`Identity:RequireConfirmedEmail` remains the sole login-policy switch. When enabled, an otherwise eligible user cannot log in until confirmation succeeds. Verification never confirms phone numbers or grants authorization.

## Email and auditing

The existing `IEmailService` sends a plain-text message titled `Verify your SchoolERP email address`, containing purpose, link, expiry, and ignore-if-unrequested guidance. Structured events record requests, resend requests, delivery results, successful verification, invalid attempts, and optional activation failure. User ID and IP may be recorded; tokens, links, security stamps, and request bodies are never logged.
