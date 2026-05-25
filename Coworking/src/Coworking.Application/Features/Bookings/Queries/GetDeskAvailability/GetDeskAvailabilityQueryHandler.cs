using Coworking.Application.Abstractions;
using Coworking.Application.Common.Exceptions;
using Coworking.Application.Features.Bookings.Queries.GetDeskAvailability.Dtos;
using Coworking.Application.Features.Bookings.Queries.GetDeskAvailability.Responses;
using Coworking.Domain.Entities;
using Coworking.Domain.Services.SlotGenerator;
using Coworking.Domain.Specifications;
using Coworking.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Application.Features.Bookings.Queries.GetDeskAvailability;

internal sealed class GetDeskAvailabilityQueryHandler(
    IAppDbContext context,
    ICoworkingRepository repository,
    ISlotGenerator slotGenerator)
    : IRequestHandler<GetDeskAvailabilityQuery, DeskAvailabilityResponse>
{
    public async Task<DeskAvailabilityResponse> Handle(GetDeskAvailabilityQuery request, CancellationToken ct)
    {
        var coworking = await GetCoworkingMetaAsync(request.DeskId, ct);
        var (startUtc, endUtc) = ToUtcBoundaries(request.DateFrom, request.DateTo, coworking.TimeZone);

        var desk = await repository.FetchDeskWithBookingsAsync(request.DeskId, startUtc, endUtc, ct)
            ?? throw new NotFoundException($"Desk {request.DeskId} not found.");

        var slots = BuildSlots(request.DateFrom, request.DateTo, coworking, desk.Bookings);

        return new DeskAvailabilityResponse
        {
            DeskId = desk.Id,
            Slots = slots
        };
    }

    /****************************************************************
     * Helpers
     *******************************************************/

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
            raw.SlotSize,
            raw.TimeZoneId);
    }

    private static (DateTimeOffset Start, DateTimeOffset End) ToUtcBoundaries(
        DateOnly dateFrom, DateOnly dateTo, TimeZoneInfo timeZone)
    {
        DateTimeOffset ToUtc(DateTime local) =>
            new(TimeZoneInfo.ConvertTimeToUtc(local, timeZone), TimeSpan.Zero);

        return (
            ToUtc(dateFrom.ToDateTime(TimeOnly.MinValue)),
            ToUtc(dateTo.AddDays(1).ToDateTime(TimeOnly.MinValue)));
    }

    private List<TimeSlotDto> BuildSlots(
        DateOnly dateFrom, DateOnly dateTo, CoworkingMeta coworking, IEnumerable<Booking> bookings)
    {
        var booked = bookings
            .Select(b => (b.StartTime, b.EndTime))
            .ToList();

        return EachDay(dateFrom, dateTo)
            .SelectMany(date => slotGenerator
                .GenerateSlots(date, coworking.OpenTime, coworking.CloseTime, coworking.SlotSize, coworking.TimeZoneId)
                .Select(slot => new TimeSlotDto(
                    slot.Start,
                    slot.End,
                    IsAvailable: !booked.Any(b => DateRangeOverlap.Check(slot.Start, slot.End, b.StartTime, b.EndTime)))))
            .OrderBy(s => s.Start)
            .ToList();
    }

    private static IEnumerable<DateOnly> EachDay(DateOnly from, DateOnly to)
    {
        for (var d = from; d <= to; d = d.AddDays(1))
            yield return d;
    }

    private sealed record CoworkingMeta(
        TimeZoneInfo TimeZone,
        TimeOnly OpenTime,
        TimeOnly CloseTime,
        SlotSize SlotSize,
        string TimeZoneId);
}
