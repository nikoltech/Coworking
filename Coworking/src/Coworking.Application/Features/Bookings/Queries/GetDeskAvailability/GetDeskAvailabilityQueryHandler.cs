using Coworking.Application.Ports;
using Coworking.Domain.Common;
using Coworking.Application.Common.Exceptions;
using Coworking.Application.Features.Bookings.Queries.GetDeskAvailability.Dtos;
using Coworking.Application.Features.Bookings.Queries.GetDeskAvailability.Responses;
using Coworking.Domain.Services.Availability;
using Coworking.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Application.Features.Bookings.Queries.GetDeskAvailability;

internal sealed class GetDeskAvailabilityQueryHandler(
    IAppDbContext context,
    ICoworkingRepository repository,
    IAvailabilityCalculator availabilityCalculator)
    : IRequestHandler<GetDeskAvailabilityQuery, DeskAvailabilityResponse>
{
    public async Task<DeskAvailabilityResponse> Handle(GetDeskAvailabilityQuery request, CancellationToken ct)
    {
        var coworking = await GetCoworkingMetaAsync(request.DeskId, ct);

        // assumes user timezone is the same as coworking timezone
        var (startUtc, endUtc) = ToUtcBoundaries(request.DateFrom, request.DateTo, coworking.TimeZone);

        var desk = await repository.FetchDeskWithBookingsAsync(request.DeskId, startUtc, endUtc, ct)
            ?? throw new NotFoundException($"Desk {request.DeskId} not found.");

        var busy = desk.Bookings
            .Select(b => (b.StartTime, b.EndTime))
            .ToList();

        var intervals = availabilityCalculator.Calculate(
            request.DateFrom,
            request.DateTo,
            coworking.OpenTime,
            coworking.CloseTime,
            coworking.TimeZone,
            busy);

        var (totalSlots, availableSlots) = CountSlots(intervals, coworking.SlotSize.Minutes);

        return new DeskAvailabilityResponse
        {
            DeskId = desk.Id,
            SlotSizeMinutes = coworking.SlotSize.Minutes,
            TotalSlots = totalSlots,
            AvailableSlots = availableSlots,
            Intervals = intervals
                .Select(i => new AvailabilityIntervalDto(i.Start, i.End, i.IsAvailable))
                .ToList()
        };
    }

    private static (int Total, int Available) CountSlots(
        IReadOnlyList<AvailabilityInterval> intervals, int slotSizeMinutes)
    {
        var total = 0;
        var available = 0;

        foreach (var interval in intervals)
        {
            var slots = (int)((interval.End - interval.Start).TotalMinutes / slotSizeMinutes);

            total += slots;

            if (interval.IsAvailable)
                available += slots;
        }

        return (total, available);
    }

    private async Task<CoworkingMeta> GetCoworkingMetaAsync(int deskId, CancellationToken ct)
    {
        var raw = await context.Set<Domain.Entities.Coworking>()
            .AsNoTracking()
            .Where(c => c.Desks.Any(d => d.Id == deskId))
            .Select(c => new { c.TimeZoneId, c.OpenTime, c.CloseTime, c.SlotSize })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Coworking for desk {deskId} not found.");

        return new CoworkingMeta(
            TimeZoneInfo.FindSystemTimeZoneById(raw.TimeZoneId),
            raw.OpenTime,
            raw.CloseTime,
            raw.SlotSize);
    }

    private static (DateTimeOffset Start, DateTimeOffset End) ToUtcBoundaries(
        DateOnly dateFrom, DateOnly dateTo, TimeZoneInfo timeZone)
    {
        // +2 days: a working window may run past midnight into the next day
        return (
            ZonedTime.FromWallClock(dateFrom.ToDateTime(TimeOnly.MinValue), timeZone),
            ZonedTime.FromWallClock(dateTo.AddDays(2).ToDateTime(TimeOnly.MinValue), timeZone));
    }

    private sealed record CoworkingMeta(
        TimeZoneInfo TimeZone,
        TimeOnly OpenTime,
        TimeOnly CloseTime,
        SlotSize SlotSize);
}
