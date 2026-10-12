# OpenAPI and Swagger

SchoolERP publishes OpenAPI 3 at `/swagger/v1/swagger.json`; Development Swagger UI is at `/swagger`. Generation uses endpoint metadata and reflection and never queries the database.

`OpenApi:Enabled` controls JSON exposure and `OpenApi:EnableUi` controls the UI. Both default to `false` in base/production configuration and are enabled in Development. Documentation never bypasses authentication or authorization.

Swagger's Bearer scheme accepts a JWT access token without a manually typed prefix. Public operations have no security requirement. Protected operations do, and permission policies appear as `x-required-permissions`. Tenant and current-school scope come from authenticated claims; there is no tenant-override header, and foreign-scope identifiers have not-found semantics.

Endpoints use feature tags and stable `Controller_Action` operation IDs. Current routes use `/api/v1`; a future v2 can coexist in another document. Fully qualified schema IDs prevent collisions. DateOnly and TimeOnly use `date` and `time` string formats.

Common responses reference `ApiErrorResponse` with safe validation, authorization, not-found, conflict, rate-limit, unexpected, and unavailable-service examples. Runtime responses carry `X-Correlation-ID`; provide it to support. See [error handling](error-handling.md) and [error codes](error-codes.md).

All examples are synthetic. The contract contains no configuration secrets or database data and is ready for future TypeScript, C#, or Flutter client generation.
