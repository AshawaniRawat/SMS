# Standard API error handling (P1-35)

All API failures use `ApiErrorResponse`: `success=false`, stable `code`, safe `message`, HTTP `status`, `correlationId`, optional field `errors`, and optional Activity `traceId`. Stack traces, exception types, SQL, constraint names, file paths, secrets, and unexpected exception messages are never returned.

Validation and malformed JSON return 400. Authentication returns 401; authorization returns 403. Hidden tenant/school resources use the same 404 behavior as nonexistent resources. Business conflicts and concurrency return 409. Rate limits return 429. Database/operation availability failures return 503. Unknown failures return `System.UnexpectedError` with 500.

FluentValidation and MVC model-state errors group de-duplicated messages by camel-case field name. The frontend `normalizeApiError` helper exposes normalized codes, messages, correlation references, and field errors suitable for React Hook Form.

The correlation ID matches the `X-Correlation-ID` response header and can be supplied to support. Expected failures are logged at Information/Warning; infrastructure and unexpected failures are logged centrally once at Error.
