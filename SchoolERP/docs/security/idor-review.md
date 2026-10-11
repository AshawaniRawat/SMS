# P1-22 IDOR review

## Reviewed surfaces

School, Campus, AcademicYear, tenant-role, role-permission, user-role assignment, registration scope, and session routes were reviewed together with all `Users`, `Roles`, `FindAsync`, identifier predicates, counts, searches, bulk operations, raw SQL, and `IgnoreQueryFilters` occurrences.

## Fixes

- Added centralized tenant/school/campus organizational-scope validation.
- Applied school and campus scope to reads, lists, counts, updates, and lifecycle operations.
- Added combined-predicate nested Campus and AcademicYear lookup routes.
- Made nested collection routes return 404 when their parent School is invisible.
- Changed foreign user-role lookup from an indistinguishable empty success to 404.
- Replaced unscoped Identity user-by-ID and state-change service methods with explicit tenant-scoped methods.
- Scoped shared system-role assignment counts through tenant users to prevent aggregate leakage.
- Changed invisible create-parent failures to resource-hiding 404 responses.

## Existing safe behavior retained

Role controllers reject a mismatched tenant route before querying. Role and user assignment queries contain explicit tenant predicates. Role-permission changes resolve the role before mappings. Session queries contain the current UserId. Tenant-owned entities retain global filters and write interception.

Approved `IgnoreQueryFilters` uses remain limited to authentication, refresh, verification, and trusted account-creation hierarchy validation. No tenant request performs a global lookup after a scoped miss. No tenant-owned bulk update/delete or tenant-sensitive raw SQL was found.

## Platform separation and technical debt

Platform user creation is a separate development-only platform route. Normal tenant endpoints require a matching tenant context and do not infer platform scope. Existing top-level Campus and AcademicYear routes remain for compatibility; new nested routes provide explicit hierarchy semantics. A future authorization phase may use permission-scope metadata for richer ABAC rules, but P1-22 intentionally enforces only the established user organization claims.
