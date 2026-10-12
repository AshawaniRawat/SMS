# Audit security

Audit payloads must never contain passwords, password hashes, tokens, authorization headers, cookies, signing keys, API keys, connection strings, security stamps, or other secrets. The centralized sanitizer removes blocked keys case-insensitively at every JSON depth. Curated snapshots avoid entity graphs and sensitive personal data; oversized content is replaced by a bounded marker.

Tenant APIs always constrain queries to the authenticated tenant before counting, paging, searching, or resolving details. School- and campus-scoped identities receive additional restrictions. The platform endpoint is separate, requires `Audit.PlatformView`, and rejects tenant identities. A missing tenant never implies platform access.

Audit rows are immutable through the domain model and no HTTP mutation endpoints exist. In production, use database grants that deny UPDATE and DELETE on `audit_logs` to the runtime role, with a separately controlled retention/archive role. Restrict audit access operationally, monitor it, encrypt backups, and set retention according to contractual and legal requirements.

IP address and user agent are collected as security context and should be covered by the product privacy notice and access policy. Actor email is masked. Correlation identifiers connect audits to operational logs without copying log payloads.
