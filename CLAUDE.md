# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Layout

The repo root (this directory) holds `Coworking.slnx`, Docker Compose files, and `.md` docs.
The actual .NET solution lives one level down:

```
Coworking/src/     - production projects
Coworking/tests/   - test projects
```

All paths below are relative to this root, e.g. `Coworking/src/Coworking.API`.

## Commands

Build and test (net10.0, no central package management, no global.json):

```bash
dotnet build Coworking.slnx
dotnet test Coworking/tests/Coworking.UnitTests
dotnet test Coworking/tests/Coworking.IntegrationTests
dotnet test Coworking/tests/Coworking.UnitTests --filter "FullyQualifiedName~CreateBookingCommandHandlerTests"
```

Run the API:

```bash
dotnet run --project Coworking/src/Coworking.API
```

Infra only (Postgres + RabbitMQ), for running the API/tests locally against `dotnet run`/`dotnet test` instead of the full Docker stack:

```bash
docker compose -f docker-compose.infra.yml up -d
```

Full containerized stack, and other Docker workflows (prod, logs, reset): see [DOCKER.md](DOCKER.md).

EF Core migrations (from repo root, see [Notes.md](Coworking/src/Coworking.Infrastructure.Persistence/Notes.md)):

```bash
dotnet ef migrations add <Name> -p Coworking/src/Coworking.Infrastructure.Persistence -s Coworking/src/Coworking.API
dotnet ef database update -p Coworking/src/Coworking.Infrastructure.Persistence -s Coworking/src/Coworking.API
```

**Integration tests need a real local Postgres** (via `docker-compose.infra.yml` or the full stack) reachable through the connection string in user secrets/`appsettings.Development.json` — there is no Testcontainers setup. `TestApiFactory` boots the real API (`WebApplicationFactory<Program>`) against a per-suite database (default `coworking_tests`), disables MassTransit hosted services, and can swap in `NoOpBookingAccessCoordinator` to bypass the in-memory lease coordinator. `TestDatabase.EnsureFreshAsync` drops and recreates the schema from the current EF model (not from migrations) the first time a run touches a given database name.

Unit tests use xunit + NSubstitute, with EF Core Sqlite (in-memory) for context-dependent tests (`TestSupport/TestAppDbContext.cs`, `TestSupport/SqliteContext.cs`).

## Architecture

Clean Architecture / CQRS. Dependency direction: `API → Application → Domain`, with `Infrastructure`, `Infrastructure.Persistence`, and `Messaging` implementing ports defined in `Application`.

- **Coworking.Domain** — entities (`Booking`, `Desk`, `Coworking`), value objects (`WorkingSchedule`, `SlotSize`, `WorkingWindow`), a small state-machine framework (`Common/StateMachine`) driving `Booking`'s lifecycle, and pure services (`AvailabilityCalculator`, `SlotGenerator`, rounding policies). No dependencies on other projects.
- **Coworking.Application** — CQRS handlers, MediatR pipeline behaviors, and the ports (interfaces) that outer layers implement. Organized as vertical slices under `Features/<Area>/{Commands,Queries}/<Name>/` (e.g. `Features/Bookings/Commands/Create/`), each folder holding the command/query, its handler, its FluentValidation validator, and nested `Requests`/`Responces`/`Dtos`/`Notifications` subfolders. Ports live under `Ports/` (`IAppDbContext`, `IBookingRepository`, `IBookingAccessCoordinator`, etc.).
- **Coworking.Infrastructure.Persistence** — EF Core `AppDbContext`, entity configurations, migrations, transaction/conflict-detection implementations (Postgres `xmin` + SQL Server variants under `Transactions/Conflicts/`).
- **Coworking.Infrastructure** — implementations of Application ports that aren't persistence: repositories (`BookingRepository`, `CoworkingRepository` — thin wrappers over `context.Set<T>()`), the in-memory booking access coordinator (`Synchronization/InMemory/`), caching (in-memory/Redis), email sending, and the Squidex CMS client wiring.
- **Coworking.Messaging** / **Coworking.Messaging.Contracts** — MassTransit/RabbitMQ setup. Three DI entry points in `Coworking.Messaging/DependencyInjection.cs`: `AddMessaging` (publishers + consumers in one bus, for a monolith), `AddMessagingPublishers` (outbox + bus, no consumers — for the service owning the DB/transactions), `AddMessagingConsumers` (consumers + inbox, no MediatR — for a dedicated notification service; shares the host's `AppDbContext`). Publishers are MediatR notification handlers; the EF Core outbox routes all publish/send calls (`UseBusOutbox`). Splitting consumers into their own deployable service is parked — see [docs/decisions/messaging-consumer-split.md](docs/decisions/messaging-consumer-split.md).
- **Coworking.External.Squidex(.Abstractions)** — generic Squidex Headless CMS HTTP client. Wired via `Coworking.Infrastructure/External/Squidex` but not yet used by any feature (per README). Has its own **[CLAUDE.md](Coworking/src/Coworking.External/Squidex/CLAUDE.md)** — read it before touching this subtree.
- **Coworking.API** — controllers, request/response DTOs + AutoMapper profile, global exception handling, Swagger, rate limiting, health checks, dev data seeding.

### Data access convention

`IAppDbContext` (`Coworking.Application/Ports/IAppDbContext.cs`) exposes only `Set<TEntity>()`, `SaveChanges(Async)`, `BeginTransactionAsync`, and `DiscardPendingChanges()` — **no raw SQL**. Simple queries call `context.Set<T>()` directly from handlers or thin repositories; repositories (`IBookingRepository`, `ICoworkingRepository`) exist for queries with real logic (overlap specifications, joins), not as a blanket abstraction over every entity.

### Booking concurrency control

Booking creation (`CreateBookingCommandHandler`) layers two independent mechanisms, in order:

1. **Application-level lease** — `IBookingAccessCoordinator.WaitIfOverlappingAsync` (in-memory implementation in `Coworking.Infrastructure/Synchronization/InMemory/`) queues overlapping requests for the same desk/interval instead of letting them all hit the database at once; a background cleaner (`BookingLockExpiryCleaner`) expires stale leases. This is advisory only — it reduces contention, it doesn't replace correctness.
2. **Database-level guarantee** — a `Serializable` transaction plus an explicit overlap check (`AnyOverlapAsync`) plus a unique/exclusion constraint, so a real conflict fails at commit (PostgreSQL SSI conflict) or via deadlock (SQL Server), never silently. `TransactionConflictRetryBehavior` (`Coworking.Application/Behaviors/`, a MediatR pipeline behavior, must run innermost) retries transient conflicts (via `IDbConflictDetector.IsTransient`, provider-specific: `PostgresConflictDetector`/`SqlServerConflictDetector`) with backoff, discarding tracked entities between attempts. After retries are exhausted it throws `TransactionConflictException` → HTTP 503 with `Retry-After`.

The solution intentionally supports **both PostgreSQL and SQL Server** conflict detection — don't assume Postgres-only when touching this path (SQL Server detection is designed but not yet wired up). See [Notes.md](Coworking/src/Coworking.Infrastructure.Persistence/Notes.md) for the EF migrations workflow, `Coworking.Benchmarks/BookingAccessCoordinatorBenchmark.cs` for coordinator performance benchmarks, and **[docs/decisions/booking-concurrency.md](docs/decisions/booking-concurrency.md)** for the full rationale (why no explicit rollback, why retry filters by predicate, lease/cleaner internals).

### MediatR pipeline (registration order matters)

Registered explicitly in `Coworking.Application/DependencyInjection.cs` (`AddBehaviorsPipeline`), in this order: `PerformanceBehavior` → `ValidationBehavior` → `DomainExceptionBehavior` → `TransactionConflictRetryBehavior` (must stay last/innermost — it needs the untranslated database exception before `DomainExceptionBehavior` or anything else touches it).

### Error handling

Application-layer exceptions (`Coworking.Application/Common/Exceptions/`: `NotFoundException`, `ConflictException`, `BusinessRuleException`, `TransactionConflictException`, `ServiceBusyException`) and `FluentValidation.ValidationException` are mapped to `ProblemDetails` by `ExceptionStatusMap` + `GlobalExceptionHandler` in `Coworking.API/Infrastructure/ExceptionHandlers/`. Domain-level `DomainException` is normally translated to `BusinessRuleException` by `DomainExceptionBehavior` inside the MediatR pipeline; the exception map also handles it directly as a fallback for errors raised outside MediatR. 503s (`TransactionConflictException`, `ServiceBusyException`) get a `Retry-After` header.

### Availability / scheduling

`WorkingSchedule` (`Coworking.Domain/ValueObjects/WorkingSchedule.cs`) owns a coworking's time zone, open/close hours, slot size, and an `IsNonStop` flag, and turns them into real UTC moments. `AvailabilityCalculator`/`SlotGenerator` (`Coworking.Domain/Services/`) compute free intervals from schedule + existing bookings; the API exposes this as an interval list (`GetDeskAvailabilityQuery` → `AvailabilityIntervalDto`), not a fixed slot grid — `SlotSize` is a client hint for interval granularity, not a generated grid of bookable slots (`SlotGenerator` itself is kept for other uses, not the availability endpoint). Full rationale (DST handling, query boundary `+2` quirk, why `SlotSize` isn't enforced): **[docs/decisions/availability-and-schedule.md](docs/decisions/availability-and-schedule.md)**.

## Quick Navigation

| Looking for | File |
|---|---|
| Solution file (project list) | `Coworking.slnx` |
| App ports (interfaces Infrastructure implements) | `Coworking/src/Coworking.Application/Ports/` |
| MediatR pipeline behaviors + registration order | `Coworking/src/Coworking.Application/Behaviors/`, `Coworking/src/Coworking.Application/DependencyInjection.cs` |
| Exception → HTTP status mapping | `Coworking/src/Coworking.API/Infrastructure/ExceptionHandlers/ExceptionStatusMap.cs` |
| Booking lifecycle / state machine | `Coworking/src/Coworking.Domain/Entities/Booking.cs`, `Coworking/src/Coworking.Domain/Common/StateMachine/` |
| Booking access coordinator (in-memory lease) | `Coworking/src/Coworking.Infrastructure/Synchronization/InMemory/` |
| DB conflict detectors (Postgres/SQL Server) | `Coworking/src/Coworking.Infrastructure.Persistence/Transactions/Conflicts/` |
| EF Core migrations + workflow notes | `Coworking/src/Coworking.Infrastructure.Persistence/Migrations/`, [Notes.md](Coworking/src/Coworking.Infrastructure.Persistence/Notes.md) |
| Connection strings, RabbitMQ, Squidex config | `Coworking/src/Coworking.API/appsettings.json` (+ `appsettings.Development.json`, user secrets) |
| RabbitMQ/MassTransit DI entry points | `Coworking/src/Coworking.Messaging/DependencyInjection.cs` |
| Rate limiting policies | `Coworking/src/Coworking.API/Infrastructure/RateLimiting/RateLimitPolicies.cs` |
| CORS / allowed origins | `Coworking/src/Coworking.API/Infrastructure/Extensions/CorsExtensions.cs` |
| Health checks | `Coworking/src/Coworking.API/Infrastructure/HealthChecks/DatabaseHealthCheck.cs` |
| Swagger setup | `Coworking/src/Coworking.API/Infrastructure/Swagger/SwaggerExtensions.cs` |
| Dev data seeding | `Coworking/src/Coworking.API/Infrastructure/Extensions/Initialization/DevDataSeeder.cs` |
| Email sending / templates | `Coworking/src/Coworking.Infrastructure/Services/Email/` |
| AutoMapper profiles | `Coworking/src/Coworking.Application/Common/Mappings/MappingProfile.cs`, `Coworking/src/Coworking.API/Mappings/ApiMappingProfile.cs` |
| Squidex CMS client | `Coworking/src/Coworking.External/Squidex/` — has its own [CLAUDE.md](Coworking/src/Coworking.External/Squidex/CLAUDE.md) |
| Docker Compose files | repo root: `docker-compose*.yml`, [DOCKER.md](DOCKER.md) |
| Architecture decision write-ups | `docs/decisions/` |

## Comments

- Before writing one, try in order: rename, extract, restructure. Comment only if all three fail.
- Delete any comment that restates adjacent code at the same level of abstraction. Keep it if it
  adds a level: intent, contract, invariant above — or a non-obvious detail below.
- Worth writing: why not the obvious approach, workarounds (link the issue), invariants the types
  can't express, business rules, spec references, code that looks wrong but is correct.
- Never: narrate the implementation, log changes ("was X, now Y"), leave commented-out code,
  write TODO without a ticket, or add comments to code you didn't change.
- Section markers: only in long functions with genuinely distinct phases. Name the phase
  ("fast path: cached"), not the category ("Helpers"). No ====, no boxes, no ALL CAPS — a blank
  line and one lowercase line is enough. If every block needs one, split the function instead.
- Docstrings: public API only. One sentence starting with the name. Contract, not implementation.
  No params that restate name and type.
- Match the comment density of the file you are editing. Default is no comment.

## Commit messages

- Conventional Commits: `type(scope): summary`
- Subject: lowercase, imperative, no period, under ~50 chars
- Bullets: `- Thing: what changed` — 2-5 words after the colon
- Cut filler: no "now", "so that", "in order to", "this allows",
  "instead of X" unless X matters
- One line per change, never wrap a bullet
- Add a prose line only when the why cannot be recovered from the diff
- No AI attribution
