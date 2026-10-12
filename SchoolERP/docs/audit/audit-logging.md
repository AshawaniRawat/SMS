# Audit logging

Audit records are durable business and security history; they do not replace operational diagnostics. `audit_logs` is append-only in the application, has no public create/update/delete API, and deliberately does not inherit the mutable auditable entity base.

The EF save interceptor classifies a curated set of entity mutations and appends the audit row in the same `SaveChanges` transaction. It ignores `AuditLog`, preventing recursion. `IAuditService` supports explicit non-entity events. Required writes are awaited; fire-and-forget is prohibited.

Each record can contain tenant, school, campus and actor identifiers; actor type; stable action/category; resource identity; outcome; safe before/after/metadata JSON; correlation/request/trace/session IDs; IP and user agent; and a UTC occurrence time. Snapshots use an allow-list, are sanitized recursively, and are bounded to 16 KiB by default.

## Query APIs

- `GET /api/v1/audit-logs` and `/{id}` require `Audit.View` and enforce tenant plus optional school/campus scope.
- `GET /api/v1/platform/audit-logs` and `/{id}` require `Audit.PlatformView` and a tenant-null platform identity.
- Filters: UTC date range, category, action, outcome, entity type, search, page number, and page size.
- Default window is 30 days; maximum range is 365 days; page size is capped at 100.

Cross-scope details return 404 because the scope predicate is applied before lookup. Foreign records never enter list counts.

## Retention and integrity

No purge runs in P1-36. `RetentionDays` is nullable and policy must be selected per customer/legal requirements; 12–24 months online followed by controlled archival is a reasonable starting point. Production database roles should grant the application INSERT and SELECT but deny UPDATE and DELETE on `audit_logs` where operationally feasible. Hash chaining, signed batches, and immutable external archives remain future enterprise enhancements.
