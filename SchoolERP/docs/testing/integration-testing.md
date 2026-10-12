# Backend integration testing

The integration suite runs the real ASP.NET Core pipeline and EF Core provider against disposable PostgreSQL. It never uses EF InMemory or SQLite and does not read a developer `DATABASE_URL` or application connection string.

## Architecture

`PostgresFixture` starts one `postgres:18.6-alpine` Testcontainers instance for the serialized PostgreSQL collection, creates an isolated `schoolerp_tests_<guid>` database, and applies every real EF migration with `MigrateAsync`. `SchoolErpWebApplicationFactory` hosts `Program` in the `Testing` environment and overrides the database connection, deterministic test JWT settings, OpenAPI exposure, logging level, and email delivery. SMTP is replaced by an in-memory fake; no external network service is contacted.

The container and generated database are disposed after the collection. Tests sharing the resettable database do not run in parallel; unit tests retain normal xUnit parallelism.

## Isolation and reset safety

`DatabaseResetter` uses Respawn's PostgreSQL adapter. It clears application data while preserving `__EFMigrationsHistory`; `IntegrationTestBase` then runs the permission and system-role seeders. Each verification query opens a fresh `SchoolErpDbContext`.

Reset is refused unless the parsed database name contains `test` or `integration`. The normal fixture always generates that name inside its disposable container. This guard is defense in depth and must not be relaxed to point tests at development or production data.

Existing scenario tests additionally generate unique tenant and resource identifiers. New reset-dependent tests should derive from `IntegrationTestBase` and remain in `PostgresCollection`.

## Authentication and test data

Real login tests use Identity and the production access-token service. `TestJwtTokenFactory` is available for focused endpoint setup: it signs a normal JWT with the same test-only issuer, audience, and 32+ byte key configured in the host. It supports `sub`, `email`, `tenant_id`, `school_id`, `campus_id`, and `jti`; permission claims are deliberately absent. Authorization continues to query persisted user-role-permission mappings on each request scope, so removing a mapping affects the next request without token reissue.

`CreateAnonymousClient`, `WithBearerToken`, `WithCorrelationId`, `ShouldBeApiErrorAsync`, and fresh-context execution helpers keep HTTP and database assertions consistent. Existing module fixtures/builders create valid domain entities through production factories.

## Running locally

A Docker-compatible runtime must be running. From `SchoolERP/backend`:

```powershell
dotnet test tests/SchoolERP.IntegrationTests/SchoolERP.IntegrationTests.csproj
dotnet test SchoolERP.sln
dotnet test tests/SchoolERP.IntegrationTests/SchoolERP.IntegrationTests.csproj --filter "Category=Security"
dotnet test tests/SchoolERP.IntegrationTests/SchoolERP.IntegrationTests.csproj --collect:"XPlat Code Coverage"
```

Tests start Testcontainers automatically; no local PostgreSQL, manual database, developer secret, or Compose service is needed. To intentionally omit container tests in an environment that cannot provide Docker, set `SCHOOLERP_TEST_SKIP_CONTAINERS=true`; skipped output states that reason. CI does not set this flag and therefore fails clearly when Docker is unavailable.

For container startup failures, first run `docker info`, verify the daemon is reachable by the current user, and rerun with detailed test logging. Testcontainers reports its readiness/startup error; do not substitute another database. No test uses sleeps or fixed host ports.

## Coverage

The suite covers migrations and seed idempotency, health, real login/JWT validation, refresh rotation/reuse and session revocation, DB-backed permissions, tenant/school/campus isolation and IDOR responses, PostgreSQL constraints and filtering, audit persistence/redaction, standard error and correlation contracts, representative module workflows, and OpenAPI security/schema metadata.
