# 001 Availability and working schedule

Status: implemented — protective spec (fixes current behavior; change it before changing the code)

Source: [availability-and-schedule.md](../../decisions/availability-and-schedule.md) and the
"Availability / scheduling" section of the root `CLAUDE.md`.

## Scope

- `GET /api/desks/{deskId}/availability` (`GetDeskAvailabilityQuery`), `AvailabilityCalculator`.
- `WorkingSchedule`: time zone, hours, non-stop flag, day windows, query boundaries.
- The schedule check on booking creation. Concurrency of creation is spec [002](../002-booking-concurrency/spec.md).

## Invariants

- **A1** The endpoint returns availability intervals plus `slotSizeMinutes`; the client expands them
  into a slot grid. The endpoint does not serialize the grid.
- **A2** `WorkingSchedule` is the single place for time-zone lookup, rounding, working-hours checks,
  day windows and availability query boundaries. Handlers work with coworking time through it,
  not `TimeZoneInfo` directly (convention, not enforced by a check).
- **A3** Windows are computed in real time, not UTC ticks. A window is anchored at `OpenTime`
  (local midnight when non-stop), one working window per local day: a DST day gives 23 or 25 hours.
- **A4** `IsNonStop` is an explicit flag on the coworking; hours are ignored when it is set.
- **A5** Both query boundaries use `GetUtcOffset`, never `ConvertTimeToUtc`.
- **A6** An unknown `TimeZoneId` throws `DomainInvariantException` (inner `TimeZoneNotFoundException`)
  and stays a 4xx via `ExceptionStatusMap`.
- **A7** `AvailabilityCalculator` is independent of `ISlotGenerator`.
- **A8** `CreateBookingCommandHandler` calls `WorkingSchedule.EnsureBoundsInWorkingHours` before
  opening the transaction.

## Intentional quirks — do not "fix"

- **Q1** `QueryBoundaries(from, to)` fetches `[from@00:00, (to+2)@00:00]`. With `+1` the last night
  of the range lost its bookings and showed as free.
- **Q2** `SlotSize` is a client-side hint, not a domain rule: nothing enforces slot alignment.
  `CreateBookingCommandValidator` checks only rounding-to-minutes, not-in-past and `end > start`.
  Returned intervals are not necessarily whole multiples of a slot; `totalSlots`/`availableSlots`
  are truncating approximations.
- **Q3** `ISlotGenerator`/`SlotGenerator` are kept for other uses; not dead code.
- **Q4** `BillableTime` is a stub (`NotImplementedException`) until payments exist. Intended semantics:
  real elapsed time.
- **Q5** A spring-forward day is one 23-hour window, a fall-back day is one 25-hour window
  (the old grid gave two windows totalling 22 hours and one stretched slot).
- **Q6** The dev seed carries awkward configurations on purpose (Night Shift, Havana Patio, Kathmandu,
  Kolkata, Lord Howe, St. John's, Chatham). Seeded bookings are anchored via the seeder's local-time
  helper, not `UtcNow.AddHours`.

## Verification

| Item | Tests |
|---|---|
| A1 | `DeskAvailabilityTests.DayHours_WithoutBookings_ReturnsOneFreeWindowPerDay` |
| A3 | `AvailabilityCalculatorTests.MultipleDays_ReturnOneWindowEachInChronologicalOrder`, `NonStopSchedule_KeepsDayContinuousAcrossTransitions`, `NightScheduleOverATransition_KeepsItsRealLength`; `WorkingScheduleTests.WindowsBetween_NightSchedule_EndsOnTheNextDay` |
| A4 | `WorkingScheduleTests.For_NonStop_IgnoresHours`; `AvailabilityCalculatorTests.NonStopSchedule_IgnoresHoursAndStartsAtMidnight`; `DeskAvailabilityTests.NonStop_ReturnsOneWindowFromMidnightToMidnight` |
| A6 | `WorkingScheduleTests.For_UnknownTimeZone_ThrowsInvariantWithTheCause`; `ExceptionMappingConventionTests.EveryExceptionIsMapped` |
| A8 | `CreateBookingWorkingHoursTests` (`Kyiv_*`, `Kathmandu_*`) |
| Q1 | `WorkingScheduleTests.QueryBoundaries_CoverTheNightTailOfTheLastDay`; `DeskAvailabilityTests.NightHours_BookingAfterMidnightOfTheLastDay_IsBusy` |
| Q2 | `AvailabilityCalculatorTests.WindowNotDivisibleBySlot_KeepsItsTail`, `WindowShorterThanASlot_IsStillReturned`, `NightScheduleNotDivisibleBySlot_KeepsItsFullLength` |
| Q3 | `SlotGeneratorTests` |
| Q4 | `WorkingScheduleTests.BillableTime_IsNotImplementedYet` |
| Q5 | `AvailabilityCalculatorTests.NonStopSchedule_KeepsDayContinuousAcrossTransitions`, `ZoneTransitioningAtMidnight_ResolvesWithoutThrowing` |
