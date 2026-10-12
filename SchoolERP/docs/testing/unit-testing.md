# Backend unit testing

SchoolERP uses one unit-test project, xUnit, and NSubstitute where a dependency boundary genuinely needs substitution. Tests are expected to be fast, deterministic, parallel-safe, and independent of PostgreSQL, Docker, network access, local configuration, and secrets.

## Structure and conventions

Production-area tests remain grouped by domain/application concern. Reusable infrastructure lives under `TestCommon`: fixed `TestIds`, safe `TestData`, `TestClock`, request-context fakes, entity builders, a structured test logger, and secret-leak assertions.

Use `MethodName_State_ExpectedResult` for new test names and keep each test focused on one behavior. Builders create valid objects through public domain factories and expose only meaningful overrides. Do not use reflection to mutate domain state.

Time-sensitive tests use `TestClock`, whose default instant is `2026-01-15T10:00:00Z`. Prefer fixed identifiers from `TestIds` over random GUIDs when identity matters to the assertion.

## Boundaries

- Unit tests may exercise domain objects, validators, mapping/redaction helpers, authorization handlers, options, and curated interceptors with suppressed writes.
- Do not use PostgreSQL, EF InMemory, SQLite, mocked `DbSet`/query providers, `WebApplicationFactory`, real HTTP, or external providers here.
- Relational constraints, migrations, query translation, and the complete HTTP pipeline belong to integration tests.
- Never use `Thread.Sleep`; advance `TestClock` explicitly.

Security-focused tests cover permission denial, tenant/resource scope, JWT claim minimization, exception safety, operational-log redaction, nested audit sanitization, and known secret markers. The `TestLoggerProvider` captures level, event ID, structured state, message, and exception without parsing console output.

## Commands

From `backend`:

```powershell
dotnet test tests/SchoolERP.UnitTests/SchoolERP.UnitTests.csproj
dotnet test tests/SchoolERP.UnitTests/SchoolERP.UnitTests.csproj --collect:"XPlat Code Coverage"
dotnet test SchoolERP.sln
```

`coverlet.collector` provides CI-ready Cobertura collection. Migrations, generated code, DTO-only models, and Program wiring may be excluded by future report configuration, but business logic should not be excluded. The initial target is meaningful coverage of at least 70% of unit-testable core logic; security and domain invariants take priority over a raw percentage.
