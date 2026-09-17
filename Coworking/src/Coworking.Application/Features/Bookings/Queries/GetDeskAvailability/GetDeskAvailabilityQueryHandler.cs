using Coworking.Application.Ports;
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
        var schedule = await GetScheduleAsync(request.DeskId, ct);

        var (startUtc, endUtc) = schedule.QueryBoundaries(request.DateFrom, request.DateTo);

        var desk = await repository.FetchDeskWithBookingsAsync(request.DeskId, startUtc, endUtc, ct)
            ?? throw new NotFoundException($"Desk {request.DeskId} not found.");

        var busy = desk.Bookings
            .Select(b => (b.StartTime, b.EndTime))
            .ToList();

        var intervals = availabilityCalculator.Calculate(request.DateFrom, request.DateTo, schedule, busy);

        var (totalSlots, availableSlots) = CountSlots(intervals, schedule.SlotSize.Minutes);

        return new DeskAvailabilityResponse
        {
            DeskId = desk.Id,
            SlotSizeMinutes = schedule.SlotSize.Minutes,
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

    private async Task<WorkingSchedule> GetScheduleAsync(int deskId, CancellationToken ct)
    {
        var raw = await context.Set<Domain.Entities.Coworking>()
            .AsNoTracking()
            .Where(c => c.Desks.Any(d => d.Id == deskId))
            .Select(c => new { c.Name, c.TimeZoneId, c.SlotSize, c.IsNonStop, c.OpenTime, c.CloseTime })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Desk {deskId} not found.");

        return WorkingSchedule.For(new Domain.Entities.Coworking
        {
            Name = raw.Name,
            TimeZoneId = raw.TimeZoneId,
            SlotSize = raw.SlotSize,
            IsNonStop = raw.IsNonStop,
            OpenTime = raw.OpenTime,
            CloseTime = raw.CloseTime
        });
    }
}