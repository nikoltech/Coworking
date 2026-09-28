# Booking concurrency

Booking creation (`CreateBookingCommandHandler`, [Create/](../../Coworking/src/Coworking.Application/Features/Bookings/Commands/Create/)) is guarded three times, in order:

1. **In-process lease** — `IBookingAccessCoordinator.WaitIfOverlappingAsync`, taken *before* the transaction opens, per desk/interval. Advisory only: it reduces contention, the database still decides.
2. **Explicit overlap check inside a `Serializable` transaction** — `BeginTransactionAsync(TransactionIsolationLevel.Serializable)`, then `AnyOverlapAsync`, then insert.
3. **Retry around the whole handler** — `TransactionConflictRetryBehavior` ([Coworking.Application/Behaviors/](../../Coworking/src/Coworking.Application/Behaviors/TransactionConflictRetryBehavior.cs)), a MediatR pipeline behavior registered **last/innermost** on purpose in `Coworking.Application/DependencyInjection.cs` (`AddBehaviorsPipeline`), so a retry re-executes the smallest possible unit.

## Must run on PostgreSQL and SQL Server

`SqlServerConflictDetector` ([Transactions/Conflicts/](../../Coworking/src/Coworking.Infrastructure.Persistence/Transactions/Conflicts/SqlServerConflictDetector.cs)) currently throws `NotImplementedException`, with the real error codes commented out (1205 deadlock, 3960 snapshot conflict, 1222 lock timeout) — the mapping is designed, just not wired up. `PostgresConflictDetector` is live: SQLSTATE `40001` (serialization failure) and `40P01` (deadlock).

## No explicit `RollbackAsync` on the error path

`EfTransactionWrapper` implements `RollbackAsync`, but the handler never calls it from a `catch`. `await using var transaction = ...` is enough — EF's `RelationalTransaction.DisposeAsync` checks whether the transaction already completed and rolls back only if needed. An explicit `RollbackAsync` inside a `catch` would itself throw and **replace** the original exception instead of chaining it, destroying the real error. Where the abort actually happens also differs by provider (Postgres aborts at `COMMIT`; SQL Server aborts at the failing *statement* for 1205/3960), so no single hand-rolled state check works for both anyway. `RollbackAsync` stays on `ITransaction` for deliberate mid-scope aborts, not error paths.

## Retry by predicate, not by exception type

EF wraps transient failures in `InvalidOperationException` ("likely due to a transient failure"), with the real provider exception (e.g. `PostgresException`) at the bottom of the chain. Filtering with `Handle<DbUpdateException>().Or<DbException>()` never matches this shape — the retry policy filters with `Policy.Handle<Exception>(dbConflictDetector.IsTransient)`, and `IsTransient` walks `ex.GetBaseException()`. On every retry, `TransactionConflictRetryBehavior` also calls `dbContext.DiscardPendingChanges()`: a rolled-back attempt leaves the previously-added `Booking` tracked as `Unchanged` with a key that no longer exists in the database, and the `DbContext` is request-scoped so the retry reuses it. EF's built-in `EnableRetryOnFailure`/`IExecutionStrategy` were deliberately left unused — they don't compose with the app-level lease + explicit transaction here.

## Lease/cleaner shape

`InMemoryBookingAccessCoordinator` + `BookingLockExpiryCleaner` ([Synchronization/InMemory/](../../Coworking/src/Coworking.Infrastructure/Synchronization/InMemory/)):

- Locking is per interval: a waiter waits only on the first holder it overlaps, then re-checks — never on other waiters, and never whole-desk/per-day/grid locking.
- `ActiveRange` wakes waiters via a `TaskCompletionSource` (not a semaphore — a semaphore let a timed-out waiter swallow the only wake-up and leave a second waiter hanging forever). Only one woken waiter proceeds; the rest re-check under the per-desk lock and wait on the new lease.
- `ttl` is the whole waiting budget for a request, across every wait it makes (`Task.WaitAsync(remaining, TimeProvider, ct)`, not a `CancellationTokenSource`). Running out throws `ServiceBusyException` → HTTP 503.
- Lease lifetime is 5 minutes from acquisition; expiry is handled by a background job (`BookingLockExpiryCleaner`), not inline in the request path — deliberate, to keep the request path free of cleanup work.

## Tests

`Coworking.IntegrationTests` boots the real host (`WebApplicationFactory<Program>`) against a separate `coworking_tests` Postgres database; schema comes from `EnsureCreated` on the current model, not migrations. Two key scenarios: with the real coordinator (proves the lease path and the 409 mapping) and with `NoOpBookingAccessCoordinator` (lets both concurrent transactions reach the database, proving the 40001 retry path actually fires).
