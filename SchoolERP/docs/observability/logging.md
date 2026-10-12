# Structured logging and correlation (P1-34)

SchoolERP uses `ILogger<T>` with Serilog as the configured provider. Console output is structured and provider-neutral; no application-log database table, Kafka, Redis, SIEM, or APM dependency is used.

Every HTTP request receives a validated `X-Correlation-ID`. Safe incoming values up to 100 characters are propagated; missing, oversized, whitespace-containing, or control-character values are replaced. The response always returns the selected value. Logs also include `HttpContext.TraceIdentifier`, and Activity `TraceId`/`SpanId` when present.

Request scopes contain correlation/request IDs and, after authentication, available `UserId`, `TenantId`, `SchoolId`, `CampusId`, method, and path. Null tenant is valid for platform and anonymous users. No database lookup is performed for enrichment. Scopes are disposed at the end of each request.

One completion event is emitted per non-health request. Successful and routine client responses use Information; requests over `LoggingFoundation:SlowRequestThresholdMs` use Warning; failures and 5xx responses use Error. Query strings, request bodies, response bodies, cookies, authorization headers, and user agents are not logged globally.

Known validation/not-found failures are not Error events. Unexpected exceptions are logged server-side with correlation and request IDs, while clients receive only a safe problem response. Database failures never expose connection strings.

Production defaults to Information with Microsoft, ASP.NET Core, EF Core, and SQL command categories at Warning. Development can override levels locally. EF sensitive-data logging remains disabled; detailed errors remain opt-in and development-only. Production retention and aggregation are responsibilities of the hosting platform. Future jobs should create explicit scopes containing JobId, TenantId, and any propagated CorrelationId.

Support workflow: obtain the correlation ID from the response, search centralized logs for `CorrelationId`, locate the request completion/exception event, and inspect its structured IDs and timing.
