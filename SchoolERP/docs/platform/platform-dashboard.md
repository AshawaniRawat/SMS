# Platform Super Admin dashboard

The platform dashboard is an aggregate operational view of the entire SchoolERP SaaS estate. Its API is `GET /api/v1/platform/dashboard`; its UI is `/platform/dashboard`. Both require the exact `Platform.ViewDashboard` permission. The backend additionally requires an authenticated platform user with no tenant assignment and opens a short-lived explicit platform execution scope. A null TenantId by itself is never authorization.

## Metrics

- **TotalTenants** counts every Tenant row, including archived tenants. Status counts use `TenantStatus`.
- **TotalSchools/Campuses/AcademicYears** count all rows across tenants under explicit platform scope. Active totals use their respective Active status.
- **TotalUsers** counts all Identity users. ActiveUsers uses `UserAccountStatus.Active`; the response also separates pending, suspended, locked, deactivated, platform, tenant, and unconfirmed-email accounts.
- **ActiveRefreshSessions** counts distinct, unexpired, unused, non-revoked session IDs.
- **OnboardingTrend** returns the configured number of calendar months, including zero-count months, based on Tenant.CreatedAt.
- **RecentTenants** returns the newest bounded set with aggregate school/campus counts in one projected query.
- **TenantsRequiringAttention** includes suspended tenants, pending tenants older than `PlatformDashboard:PendingTenantWarningDays` (default 7), and active tenants with no school.
- **SystemHealth** uses the registered PostgreSQL health check and does not call the public health endpoint.

Queries are sequential, server-aggregated, `AsNoTracking`, bounded, and do not perform a query per tenant. The response excludes user identities, contact details, configuration, tokens, and other tenant-private data. No cache is enabled yet. Billing, student analytics, tenant editing, impersonation, and audit-event feeds are outside P1-23.

The React page uses TanStack Query and the shared Axios client. It provides responsive summary cards, accessible status/trend visualizations, recent tenants, attention and health panels, skeleton loading, safe error/retry behavior, and empty states. Navigation and routing use effective permissions for UX; backend authorization remains authoritative.
