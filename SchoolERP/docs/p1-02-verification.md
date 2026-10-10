# P1-02 — PostgreSQL persistence foundation

Implemented only PostgreSQL configuration and Tenant/School/Campus foundation persistence. No P1-03, authentication workflows, CRUD endpoints or later-phase modules were added.

## Files created

Paths below are relative to SchoolERP unless stated otherwise.

- backend/dotnet-tools.json — local dotnet-ef manifest, pinned to 9.0.12.
- backend/src/SchoolERP.Application/Abstractions/ICurrentTenant.cs and IPlatformDataAccess.cs.
- backend/src/SchoolERP.Domain/Entities/Tenant.cs, School.cs and Campus.cs.
- backend/src/SchoolERP.Infrastructure/Tenancy/CurrentTenant.cs — unresolved tenant and denied platform access defaults.
- backend/src/SchoolERP.Infrastructure/Persistence/DatabaseOptions.cs — consolidated database options owned by Infrastructure.
- backend/src/SchoolERP.Infrastructure/Persistence/Configurations/AuditConfiguration.cs, TenantConfiguration.cs, SchoolConfiguration.cs and CampusConfiguration.cs.
- backend/src/SchoolERP.Infrastructure/Persistence/Interceptors/AuditableEntityInterceptor.cs.
- backend/src/SchoolERP.Infrastructure/Persistence/Seed/DatabaseSeeder.cs.
- backend/src/SchoolERP.Infrastructure/Health/PostgresHealthCheck.cs.
- backend/src/SchoolERP.Infrastructure/Persistence/Migrations/20260913135203_InitialDatabaseSetup.cs and its generated Designer.cs, plus SchoolErpDbContextModelSnapshot.cs.
- backend/src/SchoolERP.Api/Extensions/DatabaseInitializationExtensions.cs.
- backend/src/SchoolERP.Api/Middleware/DatabaseExceptionHandler.cs.
- backend/tests/SchoolERP.UnitTests/TenantTests.cs and AuditTests.cs.
- backend/tests/SchoolERP.IntegrationTests/PostgresFixture.cs and PersistenceTests.cs.
- docs/p1-02-verification.md.

## Files modified or replaced

- Infrastructure: DependencyInjection.cs, Persistence/Context/SchoolErpDbContext.cs and SchoolERP.Infrastructure.csproj.
- API: Program.cs, Extensions/DependencyInjection.cs, SchoolERP.Api.csproj and all three appsettings JSON files.
- Removed API/Configuration/DatabaseOptions.cs in favor of Infrastructure's shared persistence configuration; removed the unused Application/Abstractions/ITenantContext.cs in favor of ICurrentTenant.
- Tests: both test project files and IntegrationTests/HealthTests.cs. ApiResponseTests.cs remains unchanged.
- docker-compose.yml, README.md, docs/architecture.md, scripts/verify.ps1 and .github/workflows/ci.yml.
- Repository-level C:/Project/SMS/.github/workflows/ci.yml also enables full Testcontainers verification for this nested project.
- Frontend files and P1-01 historical verification remain unchanged.

## Packages

New references:

| Project | Package | Version |
| --- | --- | --- |
| Infrastructure | Microsoft.EntityFrameworkCore | 9.0.12 |
| Infrastructure | Microsoft.EntityFrameworkCore.Tools | 9.0.12 |
| Infrastructure | Microsoft.Extensions.Diagnostics.HealthChecks | 9.0.12 |
| Infrastructure | Microsoft.Extensions.Hosting.Abstractions | 9.0.12 |
| Infrastructure | Microsoft.Extensions.Options.DataAnnotations | 9.0.12 |
| API | Microsoft.EntityFrameworkCore.Design | 9.0.12 |
| IntegrationTests | Testcontainers.PostgreSql | 4.15.0 |

Infrastructure uses EF Design 9.0.12 and Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4. Design/Tools references are private assets. Framework targets net9.0. UnitTests references Infrastructure to exercise actual audit behavior.

## Schema

EF-generated migration: **20260913135203_InitialDatabaseSetup**.

Verified tables: tenants, schools, campuses, __EFMigrationsHistory.

Includes unique tenant slug, unique school (tenant_id, code), unique campus (school_id, code), required/max-length fields, nonblank checks, slug format check and indexed foreign keys. Campus's composite school FK enforces tenant agreement. Parent deletion is restricted. Dates use PostgreSQL timestamptz; table/column names are snake_case. Migration SQL was not manually edited.

## Commands and results

Executed on 2026-09-13 with .NET SDK 10.0.303:

```powershell
# From backend
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 9.0.12
dotnet restore
dotnet build --no-restore
dotnet ef migrations add InitialDatabaseSetup --project src/SchoolERP.Infrastructure --startup-project src/SchoolERP.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/SchoolERP.Infrastructure --startup-project src/SchoolERP.Api --no-build
dotnet test --no-build
dotnet run --no-build --no-launch-profile --project src/SchoolERP.Api
```

Package additions used dotnet add package in the projects listed above. The native verification connection was supplied only through process environment variables with a generated temporary password.

- Final restore/build succeeded: **0 warnings, 0 errors**.
- Final tests: **16 unit + 19 integration = 35 passed; 0 failed; 0 skipped**.
- Real PostgreSQL testing used the installed PostgreSQL 18 binaries to initialize a separate, loopback-only temporary cluster on port 55432. Existing local PostgreSQL databases were not modified.
- The fixture created/migrated/dropped its own randomly named test database. It verified migration/model consistency, connectivity, inserts, uniqueness, cross-tenant FK rejection, restricted deletion, per-context query isolation, rejected mismatched/missing-tenant writes, valid tenant writes, UTC audit updates, optional/idempotent seeding and database-level slug validation.
- WebApplicationFactory verified healthy PostgreSQL responses, safe unhealthy HTTP 503, development Swagger, safe database exceptions and production refusal to migrate/seed even when flags are enabled.
- The isolated schoolerp database was migrated with EF CLI. psql confirmed all four tables, indexes/FKs and the migration history row.
- Running /api/health returned HTTP 200 with {"status":"Healthy"}; development OpenAPI returned HTTP 200.
- Official standalone Docker Compose v5.5.1 validated the Compose configuration successfully. Compose up -d postgres was attempted and failed because the Docker daemon's Windows pipe does not exist. **Docker container startup remains unverified on this machine.** Install/start Docker Desktop and run the README commands to complete that environment check.
- The temporary verification PostgreSQL cluster was stopped; the existing PostgreSQL service remained running. Automatic approval review blocked cleanup deletion with "blocked by policy" and no detailed reason. Temporary cluster files and generated verification password files remain in the system temporary directory, outside the repository. No production credentials were used.

## Remaining extension points

- Implement validated identity/tenant membership resolution in ICurrentTenant in a later phase. No request headers or JWT tokens are trusted yet.
- Implement authorization-checked platform access; the default is always denied. Raw SQL, IgnoreQueryFilters and direct DB access remain privileged and are not protected by EF filters.
- Populate CreatedBy/UpdatedBy from a future validated current-user service.
- Use controlled production migration deployment and a secret store; startup migration/seed logic is development-only.
- Run the Testcontainers mode on a Docker-enabled host. It is configured in CI; local native PostgreSQL verification passed.

Development passwords in committed examples are local-only defaults. No temporary or production database secrets were committed.
