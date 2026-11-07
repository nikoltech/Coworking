# 002 Booking concurrency

Status: implemented — protective spec (fixes current behavior; change it before changing the code)

Source: [booking-concurrency.md](../../decisions/booking-concurrency.md) and the
"Booking concurrency control" and "MediatR pipeline" sections of the root `CLAUDE.md`.

## Scope

- Booking creation: `CreateBookingCommandHandler`.
- Lease: `IBookingAccessCoordinator`, `InMemoryBookingAccessCoordinator`, `BookingLockExpiryCleaner`.
- Database guarantee and retry: `Serializable` transaction, `AnyOverlapAsync`,
  `TransactionConflictRetryBehavior`, `IDbConflictDetector`.
- The schedule check on creation is spec [001](../001-availability-and-schedule/spec.md).

## Invariants

- **C1** Creation is guarded three times, in order: the in-process lease, taken before the transaction
  opens; the explicit overlap check (`AnyOverlapAsync`) and insert inside a `Serializable` transaction;
  the retry around the whole handler.
- **C2** A unique/exclusion constraint backs the overlap check. A real conflict fails at commit
  (PostgreSQL SSI) or via deadlock (SQL Server), never silently.
- **C3** Pipeline order: `PerformanceBehavior` → `ValidationBehavior` → `DomainExceptionBehavior` →
  `TransactionConflictRetryBehavior`, which stays last/innermost.
- **C4** Retry filters by predicate: `Policy.Handle<Exception>(dbConflictDetector.IsTransient)`,
  and `IsTransient` walks `ex.GetBaseException()`.
- **C5** On every retry `TransactionConflictRetryBehavior` calls `dbContext.DiscardPendingChanges()`.
- **C6** `PostgresConflictDetector` treats SQLSTATE `40001` and `40P01` as transient.
- **C7** After retries are exhausted: `TransactionConflictException` → HTTP 503 with `Retry-After`.
- **C8** Locking is per interval: a waiter waits only on the first holder it overlaps, then re-checks;
  never on other waiters; never whole-desk, per-day or grid locking.
- **C9** `ActiveRange` wakes waiters via a `TaskCompletionSource`. Only one woken waiter proceeds;
  the rest re-check under the per-desk lock and wait on the new lease.
- **C10** `ttl` is the whole waiting budget of a request, across every wait. Running out throws
  `ServiceBusyException` → HTTP 503 with `Retry-After`.
- **C11** A lease lives 5 minutes from acquisition. Expiry is handled by `BookingLockExpiryCleaner`
  in the background, not in the request path.

## Intentional quirks — do not "fix"

- **Q1** No explicit `RollbackAsync` on the error path; `await using` disposal rolls back when needed.
  A rollback in a `catch` would replace the original exception. `RollbackAsync` stays on `ITransaction`
  for deliberate mid-scope aborts.
- **Q2** EF `EnableRetryOnFailure`/`IExecutionStrategy` are deliberately unused: they do not compose
  with the app-level lease and the explicit transaction.
- **Q3** The solution must run on PostgreSQL and SQL Server. `SqlServerConflictDetector` throws
  `NotImplementedException` with codes 1205, 3960, 1222 designed but not wired. Do not assume
  PostgreSQL only.
- **Q4** The lease is advisory: it reduces contention, the database decides.
- **Q5** Wake-up uses a `TaskCompletionSource`, not a semaphore: a semaphore let a timed-out waiter
  swallow the only wake-up.

## Verification

`Coworking.IntegrationTests` boots the real host against a separate `coworking_tests` PostgreSQL
database, schema from `EnsureCreated`. Two key scenarios: with the real coordinator (lease path and
409 mapping) and with `NoOpBookingAccessCoordinator` (both transactions reach the database; the
`40001` retry path fires).

| Item | Tests |
|---|---|
| C1, C2 | `ConcurrentBookingTests.OverlappingRequests_WithCoordinator_LeaveExactlyOneWinner`, `OverlappingRequests_WithoutCoordinator_LeaveExactlyOneWinner` |
| C3 | `PipelineOrderTests.TransactionConflictRetryBehavior_IsRegisteredLast` |
| C4 | `PostgresConflictDetectorTests.WrappedSerializationConflict_IsTransient`; `TransactionConflictRetryBehaviorTests.TransientFailure_IsRetriedAndSucceeds`, `NonTransientFailure_IsNotRetried`; `RetryPathTests.SerializationFailureAtCommit_IsRetriedAndTheBookingIsCreated` |
| C5 | `TransactionConflictRetryBehaviorTests.TransientFailure_IsRetriedAndSucceeds`, `NonTransientFailure_IsNotRetried` |
| C6 | `PostgresConflictDetectorTests.SerializationConflicts_AreTransient` |
| C7 | `RetryPathTests.RetriesExhausted_Returns503WithRetryAfter` |
| C8 | `BookingAccessCoordinatorTests` (all); `BookingAccessCoordinatorQueueTests.Queue_ServesOverlappingRequestsOneByOne`, `Request_IsCheckedAgainstHoldersOnly_NotAgainstWaiters` |
| C9, Q5 | `BookingAccessCoordinatorQueueTests.BudgetRunsOut_ThrowsServiceBusy_AndDoesNotHoldBackOthers` |
| C10 | `BookingAccessCoordinatorQueueTests.Budget_CoversTheWholeRequest_NotEachWait`, `BudgetRunsOut_ThrowsServiceBusy_AndDoesNotHoldBackOthers` |
| C11 | `BookingAccessCoordinatorQueueTests.Cleanup_ReleasesLeaseExactlyAtItsLifetime`, `Cleanup_ExpiredLeaseReleasedLater_DoesNotFreeItsSuccessor` |
