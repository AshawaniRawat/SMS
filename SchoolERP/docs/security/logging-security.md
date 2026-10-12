# Logging security policy

Never log passwords, password confirmation, JWTs, access/refresh/reset/email-verification tokens, cookies, authorization headers, signing keys, database passwords/connection strings, API keys, student medical data, documents, or arbitrary request/response bodies. Prefer identifiers over email, phone, address, or entity/DTO serialization.

Unsafe: `logger.LogInformation("Login {@Request}", request)`.

Safe: `logger.LogInformation("Login succeeded for UserId {UserId} and SessionId {SessionId}", userId, sessionId)`.

Sensitive key names are matched case-insensitively and fully replaced with `[REDACTED]`. Email and phone helpers provide masking only when operationally unavoidable. Signing-key validation reports policy failure, never the supplied key. EF sensitive-data logging is disabled by default and in production.

Application logs are operational diagnostics. They do not replace durable business/security audit history.
