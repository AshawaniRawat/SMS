# API error codes

- Validation: `Validation.Failed`, `Validation.InvalidJson`, `Validation.ConstraintFailed`
- Authorization: `Authorization.Unauthenticated`, `Authorization.PermissionDenied`
- Tenant: `Tenant.ContextMissing`, `Tenant.ScopeViolation`
- HTTP: `Http.RouteNotFound`, `Http.MethodNotAllowed`
- Infrastructure: `Infrastructure.DatabaseUnavailable`, `Infrastructure.OperationTimeout`
- Concurrency: `Concurrency.Conflict`
- System: `System.UnexpectedError`
- Modules: stable `Tenant.*`, `School.*`, `Campus.*`, `AcademicYear.*`, `Class.*`, `Section.*`, `Subject.*`, `Department.*`, `House.*`, and `Calendar.*` codes are retained.

Codes are stable integration identifiers; user-facing text may evolve.
