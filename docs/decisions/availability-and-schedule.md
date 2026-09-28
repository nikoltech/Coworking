# Availability & working schedule

`GET /api/desks/{deskId}/availability` ([GetDeskAvailabilityQuery](../../Coworking/src/Coworking.Application/Features/Bookings/Queries/GetDeskAvailability/)) returns availability **intervals** plus `slotSizeMinutes` — the client expands them into a slot grid; the endpoint no longer serializes the grid itself.

**Why:** under load the old endpoint expanded up to ~13,000 slots per request (~78,000 `TimeZoneInfo` calls, ~2 MB JSON), while a day is really described by 3-5 segments. There were no external API consumers at the time, so the contract was changed freely.

`SlotSize` is a client-side hint, not a domain rule: nothing enforces slot alignment. `CreateBookingCommandValidator` only checks rounding-to-minutes, not-in-past, and `end > start`. Consequence: intervals returned by availability are not necessarily whole multiples of a slot, and `totalSlots`/`availableSlots` in the response are truncating approximations. `ISlotGenerator`/`SlotGenerator` ([Services/SlotGenerator/](../../Coworking/src/Coworking.Domain/Services/SlotGenerator/)) are kept for other uses — do not delete as dead code just because the availability endpoint stopped using them.

`AvailabilityCalculator` ([Services/Availability/](../../Coworking/src/Coworking.Domain/Services/Availability/AvailabilityCalculator.cs)) is independent of `ISlotGenerator` on purpose — a replacement of both the service and the approach, not a wrapper around it.

## WorkingSchedule

`WorkingSchedule` ([ValueObjects/WorkingSchedule.cs](../../Coworking/src/Coworking.Domain/ValueObjects/WorkingSchedule.cs)) is the single place for time-zone lookup, rounding, working-hours checks, day windows, and availability query boundaries. Handlers work with coworking time through it, not `TimeZoneInfo` directly — convention only, no architecture test or banned-API analyzer enforces it (deliberately: not worth blocking `TimeZoneInfo` project-wide for one feature).

- Windows are computed in real time, not UTC ticks (UTC ticks broke on `Nepal +05:45`). A window is anchored at `OpenTime` (local midnight when non-stop) and steps in real time — one working window per local day, so a DST day gives 23 or 25 hours in `WindowsBetween`, not a fixed 24.
- `IsNonStop` is an explicit flag on the coworking; hours are ignored when set (not a `00:00-00:00` convention).
- `QueryBoundaries(from, to)` fetches `[from@00:00, (to+2)@00:00]` — the `+2` (not `+1`) matters because a night window can run past midnight; with `+1` the last night of the range silently lost its bookings and showed as free.
- Both boundaries use `GetUtcOffset`, never `ConvertTimeToUtc`, which throws on a local midnight that does not exist (verified case: `2027-03-14 00:00 America/Havana`).
- Unknown `TimeZoneId` throws `DomainInvariantException` (inner `TimeZoneNotFoundException`) — stays a 4xx via `ExceptionStatusMap`.
- `BillableTime` is a stub (`NotImplementedException`) until payments exist; intended semantics is real elapsed time — DST hours burn or extend, the customer pays for what was reserved.

Deliberate divergences from the old slot grid, both recovering genuinely bookable time the grid used to drop: a spring-forward day is one 23-hour window (the old grid gave two windows totalling 22h); a fall-back day is one 25-hour window (the old grid gave one stretched slot).

The dev seed (`DevDataSeeder`) intentionally carries awkward configurations to exercise this: Night Shift (`22:00-06:30`, Europe/Kyiv), Havana Patio (`America/Havana`, 24/7), plus Kathmandu, Kolkata, Lord Howe, St. John's, and Chatham. Seeded bookings are anchored via the seeder's local-time helper, not `UtcNow.AddHours`, or they drift out of a night window depending on when the seed runs.

See also [booking-concurrency.md](booking-concurrency.md) for the write side of the same feature (`CreateBookingCommandHandler` calls `WorkingSchedule.EnsureBoundsInWorkingHours` before opening the transaction).
