# SchoolERP

P1-01 application structure and P1-02 PostgreSQL persistence foundation for a multi-tenant School Management / School ERP SaaS. Includes a mock dashboard, login preview, Tenant/School/Campus foundation entities, EF migrations, auditing and database-backed health checks. Authentication and business modules belong to later phases.

## Architecture and technologies

Modular monolith using Clean Architecture, SOLID and dependency injection. .NET 9 Web API, EF Core, Npgsql/PostgreSQL, Identity/JWT preparation, FluentValidation, Swagger, Serilog and xUnit. React 19, TypeScript, Vite, Tailwind CSS 4 with its Vite plugin, React Router, TanStack Query, React Hook Form, Zod and Axios. The frontend includes aliases, CSS variables and components.json for future shadcn/ui components.

See [architecture](docs/architecture.md) for dependencies and tenant-isolation requirements.

Authentication includes rotating refresh-token sessions and self-service logout/session revocation. See [logout and sessions](docs/security/logout-and-sessions.md) for the API flow and the stateless access-token limitation.
Password recovery uses short-lived ASP.NET Identity tokens, enumeration-safe responses, and session revocation. See [password recovery](docs/security/password-recovery.md).
Email ownership confirmation uses a separate 24-hour Identity token provider and enumeration-safe resend flow. See [email verification](docs/security/email-verification.md).
Role management supports protected system roles, tenant custom roles, lifecycle controls, and scoped Identity assignments. See [roles](docs/security/roles.md).

## Folder structure

```text
SchoolERP/
  backend/SchoolERP.sln
  backend/src/SchoolERP.{Api,Application,Domain,Infrastructure,Shared}
  backend/tests/SchoolERP.{UnitTests,IntegrationTests}
  frontend/src/     # feature folders, routes, layout, API client and pages
  docs/ docker/ scripts/ .github/workflows/
  docker-compose.yml
```

## Prerequisites

Windows PowerShell, .NET 9 SDK, Node.js 22.12+ (24 recommended), npm, Docker Desktop with Compose and Git. Local PostgreSQL 18 can replace Docker.

## PostgreSQL and Docker

Run from C:\Project\SMS\SchoolERP:

```powershell
docker compose config
docker compose up -d postgres
docker compose ps
docker compose logs postgres
docker compose down
```

PostgreSQL 18.6 listens on localhost:5432 with database schoolerp, username postgres and password postgres. These are local defaults, never production credentials. Named volume schoolerp_postgres_data persists after compose down. PostgreSQL 18 stores data under the /var/lib/postgresql parent mount. The old P1-01 PostgreSQL 17 volume is not reused or deleted; an existing populated database requires a deliberate dump/restore upgrade. See the [official image storage documentation](https://hub.docker.com/_/postgres).

With a native installation, create the schoolerp database and configure your local connection string. The initial migration creates tenants, schools, campuses and __EFMigrationsHistory. No later-phase business tables or Identity tables are created.

## Backend setup and run

```powershell
cd C:\Project\SMS\SchoolERP\backend
dotnet restore
dotnet tool restore
dotnet build
dotnet ef database update --project src/SchoolERP.Infrastructure --startup-project src/SchoolERP.Api
dotnet run --project src/SchoolERP.Api
```

API: http://localhost:5000. Development Swagger UI opens automatically at http://localhost:5000/swagger and includes JWT Bearer authorization through its **Authorize** button. The OpenAPI document is available at http://localhost:5000/swagger/v1/swagger.json. Health: http://localhost:5000/api/health verifies PostgreSQL connectivity and returns HTTP 200 with {"status":"Healthy"}, or HTTP 503 with {"status":"Unhealthy"}. It does not expose connection details or verify every schema object. Future controllers inherit ApiControllerBase for api/v1/[controller] routes.

Development applies migrations at startup by default, using Database:ApplyMigrationsOnStartup=true. SeedDevelopmentData defaults to false. These actions are refused outside Development regardless of flags. Production must deploy migrations separately with controlled permissions. EnsureCreated is never used.

The InitialDatabaseSetup migration already exists. To reproduce its creation in a fresh branch before it exists, use:

```powershell
dotnet ef migrations add InitialDatabaseSetup --project src/SchoolERP.Infrastructure --startup-project src/SchoolERP.Api --output-dir Persistence/Migrations
```

For subsequent schema changes use a new descriptive migration name, then the database update command above. The local dotnet-ef tool is pinned to the EF Core version; no design-time factory is needed because API startup discovery works.

## Frontend setup and run

In a second terminal:

```powershell
cd C:\Project\SMS\SchoolERP\frontend
Copy-Item .env.example .env
npm install
npm run dev
```

Open http://localhost:5173. Routes: /, /login, /unauthorized, /not-found and a wildcard fallback. All dashboard counts are mock data. Login validates inputs locally and sends nothing. Logout is a disabled placeholder. Mobile navigation includes a toggle and overlay. Add the dark class to html for dark mode; override --primary and --primary-foreground for branding. Production hosting must rewrite SPA routes to index.html.

## Configuration and environment variables

appsettings.json contains safe defaults; appsettings.Development.json contains local-only PostgreSQL credentials and localhost CORS. appsettings.example.json documents the shape. Production has no default connection string or allowed CORS origins. Never place real secrets in these files.

```powershell
cd C:\Project\SMS\SchoolERP\backend
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=schoolerp;Username=postgres;Password=YOUR_LOCAL_PASSWORD" --project src/SchoolERP.Api
$env:ConnectionStrings__DefaultConnection = "YOUR_DEPLOYMENT_CONNECTION_STRING"
$env:Cors__AllowedOrigins__0 = "https://your-frontend.example"
$env:AllowedHosts = "your-api.example"
```

Typed options: Database, Jwt, Cors, Storage, Email. JWT authentication and email/storage services are unimplemented; options alone do not enable them. Frontend VITE_API_BASE_URL defaults to http://localhost:5000/api; all VITE variables are public browser configuration. .env files are ignored. Use a deployment secret store for production values. Swagger is enabled only in Development and production errors do not expose stack traces.

Database options live in Infrastructure and bind from the Database section. ConnectionStringName=DefaultConnection, MaxRetryCount=3, MaxRetryDelaySeconds=5, CommandTimeoutSeconds=30. Retry count accepts 0–5; delay 1–30 seconds; command timeout 1–300 seconds. EnableSensitiveDataLogging and EnableDetailedErrors default to false and are honored only in Development. Production ApplyMigrationsOnStartup and SeedDevelopmentData defaults are false. Overrides use Database__ApplyMigrationsOnStartup, Database__SeedDevelopmentData, etc. Do not use a DatabaseOptions section name; the class is DatabaseOptions, the configuration section is Database.

EF sensitive-data logging and detailed errors remain disabled by default. Provider exceptions are logged server-side through Serilog and returned as safe ProblemDetails. Never log configuration objects or connection strings.

## Build and testing

```powershell
cd C:\Project\SMS\SchoolERP
./scripts/verify.ps1
# Or independently:
dotnet test backend/SchoolERP.sln
npm run build --prefix frontend
Invoke-RestMethod http://localhost:5000/api/health
```

xUnit verifies ApiResponse creation, slug rules and sync/async audit timestamps. Tests for failed health and safe database errors do not need PostgreSQL. Database integration tests validate real PostgreSQL migrations, relationships, unique constraints, tenant isolation and healthy API responses. No EF in-memory provider is used.

Enable the full suite with Docker Desktop running:

```powershell
cd C:\Project\SMS\SchoolERP\backend
$env:SCHOOLERP_TEST_USE_CONTAINERS = "true"
dotnet test
Remove-Item Env:SCHOOLERP_TEST_USE_CONTAINERS
```

Without Docker, point tests at a native PostgreSQL administration connection supplied securely in your terminal:

```powershell
$env:SCHOOLERP_TEST_POSTGRES = "Host=localhost;Port=5432;Database=postgres;Username=YOUR_TEST_ROLE;Password=YOUR_TEST_PASSWORD"
dotnet test
Remove-Item Env:SCHOOLERP_TEST_POSTGRES
```

The test role needs CREATE DATABASE permission. The fixture creates, migrates and drops only its own schoolerp_tests_<random> database. It never migrates or deletes the supplied database. If neither test setting is supplied, database-dependent tests are explicitly skipped; this is not a full persistence verification. CI enables Testcontainers and runs the complete suite. Commit frontend/package-lock.json for reproducible npm ci installs.

## Tenant persistence behavior

School and Campus implement ITenantEntity; BaseEntity remains tenant-neutral. A missing ICurrentTenant returns no school/campus rows. Normal writes require a matching tenant and cannot change TenantId. Tenant itself is a platform entity. Campus uses a composite (tenant_id, school_id) foreign key to School's (tenant_id, id), so the database rejects cross-tenant school links. School codes are unique per tenant; campus codes per school. Codes are case-sensitive and not normalized in this phase. Slugs require lowercase ASCII letters/digits with single hyphens between segments, enforced in Domain and PostgreSQL.

CurrentTenant deliberately resolves no tenant until authentication is implemented. IPlatformDataAccess is a privileged extension point with an always-false default implementation. There is no public bypass endpoint/header. Future administrative services must verify authorization before replacing it. IgnoreQueryFilters, raw SQL and direct database access can bypass EF protections and must remain privileged infrastructure operations. Never reuse a DbContext across tenant boundaries. Restricted deletion prevents silently cascading tenant/school deletion.

## PostgreSQL troubleshooting

- **Port 5432 in use:** check Get-NetTCPConnection -LocalPort 5432. Run native PostgreSQL or the Compose instance, or change the Compose host port and connection string together.
- **Password authentication failure:** check user secrets/environment overrides and the actual database role. Changing POSTGRES_PASSWORD does not change passwords in an existing initialized volume. Do not delete a populated volume to fix credentials.
- **Database unavailable:** run docker compose ps and docker compose logs postgres; verify host, port and that the schoolerp database exists. localhost is correct for an API running on the host; a future containerized API would use postgres as host.
- **Migration failure:** run dotnet tool restore, verify CREATE/ALTER permissions, check dotnet ef migrations list --project src/SchoolERP.Infrastructure --startup-project src/SchoolERP.Api and inspect server-side logs. Apply the committed migration rather than EnsureCreated.
- **Docker not running:** start Docker Desktop in Linux-container mode, then run docker info and docker compose up -d postgres. Native PostgreSQL is an alternative; Testcontainers requires Docker.
- **Startup migration intentionally disabled:** set Database__ApplyMigrationsOnStartup=false and apply migrations with the CLI. Database-backed health remains unhealthy until PostgreSQL is reachable.

## Coding conventions

Nullable reference types, implicit usings and warnings-as-errors are enabled. Use PascalCase for C# types, camelCase for TypeScript values and feature-based folders. Keep HTTP concerns in API, use cases in Application, business rules in Domain and persistence in Infrastructure. Use async for I/O. Avoid repositories that merely wrap EF Core. Keep BaseEntity tenant-neutral; implement ITenantEntity on tenant-owned types and retain query/write isolation. Implement validated identity and tenant membership before exposing tenant workflows.

## Future modules

Authentication, academic years, users, roles and permissions retain placeholder folders. Tenant, School and Campus are persistence foundations only, with no CRUD APIs or workflows. Students, fees, attendance, exams, HR, payroll, LMS and transport are later phases. P1-03 and later functionality are not implemented.
# SchoolERP

## Development JWT setup

The API requires an external JWT signing key. From `backend`, create a random development secret and store it with .NET user-secrets:

```powershell
$key = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
dotnet user-secrets set "Jwt:SigningKey" $key --project src/SchoolERP.Api
```

`Jwt:Issuer` defaults to `SchoolERP.Api` and `Jwt:Audience` to `SchoolERP.Client`. Production must use `Jwt__SigningKey` or a managed secret store and HTTPS; never commit a real key.
# Development logging

Backend logs are written to the console as structured events. API responses include `X-Correlation-ID`; provide this value when reporting an error. Change local levels with `Serilog__MinimumLevel__Default` or development configuration. Never place secrets in logging configuration or commit production credentials.

# API documentation

In Development, open `/swagger` for Swagger UI or `/swagger/v1/swagger.json` for the OpenAPI 3 contract. Obtain a development access token through login, select **Authorize**, and paste only the access token. Swagger uses real authentication and permissions. Production JSON and UI exposure are disabled by default through `OpenApi` configuration.

# Testing

Run backend unit tests with `dotnet test tests/SchoolERP.UnitTests/SchoolERP.UnitTests.csproj` from `backend`. Coverage is available with `--collect:"XPlat Code Coverage"`. See [unit-testing guidance](docs/testing/unit-testing.md).

## Integration tests

Run `dotnet test tests/SchoolERP.IntegrationTests/SchoolERP.IntegrationTests.csproj` from `backend`. A running Docker-compatible runtime is required; Testcontainers provisions disposable PostgreSQL and applies the real migrations automatically. No locally installed database or developer secret is used. See [integration-testing guidance](docs/testing/integration-testing.md) for isolation, authentication, filtering, and debugging details.

