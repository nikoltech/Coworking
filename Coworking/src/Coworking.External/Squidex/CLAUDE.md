# CLAUDE.md — Coworking.External.Squidex

Scope: this subtree only. For the rest of the solution see the [root CLAUDE.md](../../../../CLAUDE.md).

Full API/usage docs (config, locales, retries, components, assets, webhooks, extending) live in **[ReadMe.md](Coworking.External.Squidex/ReadMe.md)** — read that first for how to use the client. This file covers only what the README doesn't: project layout and status in this solution, plus test setup.

## What this is

A generic, reusable Squidex Headless CMS client — not booking-domain code. Per the root README it's wired up but **not yet used by any feature**; the only real consumer today is `Coworking.Infrastructure/External/Squidex/` (typed `City`/`Email` repositories behind `IMainSquidexContext`, an `Application`-layer port — see `Coworking.Application/Ports/Squidex/IMainSquidexContext.cs` and `Coworking.Infrastructure/External/Squidex/Contexts/MainSquidexContext.cs`). Don't leak booking concepts into this subtree.

## Project split

- **`.Abstractions`** — interfaces, DTOs, options. No implementation, depends on nothing else in the solution.
- **`Coworking.External.Squidex`** — the client (HTTP, auth, context, DI). References `.Abstractions` only.
- **`.UnitTests`** — `InternalsVisibleTo` from the main project, so it can test `internal` classes (e.g. `SquidexApiClient`) directly.

## Open: the ETag reaches the caller from one method only

`GetByIdIfChangedAsync` returns the `ETag` header in a tuple. Every other read — `CreateAsync`, `GetByIdAsync`, the query methods — returns `ContentDto<T>`, which carries `version` and no ETag. So an ordinary read-then-write has nothing to hand to `knownETag` except a hand-formatted `$"\"{dto.Version}\""`; Squidex accepts both forms in `If-Match`, but nothing in the API says so.

Settling it most likely means putting the ETag on `ContentDto<T>`, filled from the response header, which also removes the tuple. It touches every read path — decide before adding further conditional methods.

## Testing

`RichardSzalay.MockHttp` mocks `HttpClient` at the handler level (`Helpers/MockHttpExtensions.cs`). **`FluentAssertions` is pinned to `6.12.*` here** — the root README's "Licensing watch" flags that v8+ moved to commercial licensing; don't bump past v7 without checking the license (this is the only project in the solution using FluentAssertions — others use plain xunit asserts).
