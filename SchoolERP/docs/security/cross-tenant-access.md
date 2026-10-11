# Cross-tenant application access

Tenant-facing APIs treat an invisible resource and a nonexistent resource identically: both return `404 Not Found`. Services never follow a scoped miss with `IgnoreQueryFilters` or another global existence query.

Tenant-owned entities use EF tenant filters plus application-layer scoping. School, Campus, and AcademicYear updates and lifecycle operations first load a visible tracked entity. Nested Campus and AcademicYear routes use a combined resource ID and School ID predicate. Nested collections verify that the parent School is visible before returning results, preventing an invisible parent from looking like an empty collection.

`ApplicationUser` and `ApplicationRole` are Identity entities and intentionally have no global tenant filter. Tenant administration therefore includes `TenantId` in every user and custom-role lookup. Role-permission mutation first resolves a tenant-visible mutable role. Session listing and revocation remain restricted to the authenticated `UserId`.

Request ownership fields are not mapped onto existing entities. Tenant IDs come from authenticated context; nested route IDs are authoritative; creation verifies parent visibility. Tenant, school, and campus organizational scope is evaluated independently of permission authorization. Platform administration remains on separately protected platform routes and cannot be enabled by a missing tenant claim.

Pagination, count, search, and uniqueness queries start from scoped queryables, so foreign rows cannot influence totals or search results. Global email uniqueness is the documented exception used by account creation and authentication.
