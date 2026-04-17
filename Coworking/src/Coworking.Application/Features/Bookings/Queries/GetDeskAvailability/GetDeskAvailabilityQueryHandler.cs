using Coworking.Application.Common.Exceptions;
using Coworking.Application.Common.Interfaces;
using Coworking.Application.Features.Bookings.Queries.GetDeskAvailability.Dtos;
using Coworking.Application.Features.Bookings.Queries.GetDeskAvailability.Responces;
using Coworking.Domain.Services.SlotGenerator;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Application.Features.Bookings.Queries.GetDeskAvailability;

// TODO: Revise logic! Is this availability per day or per whole time?
internal sealed class GetDeskAvailabilityQueryHandler(
    ICoworkingRepository repository,
    ISlotGenerator slotGenerator)
    : IRequestHandler<GetDeskAvailabilityQuery, DeskAvailabilityResponse>
{
    public async Task<DeskAvailabilityResponse> Handle(GetDeskAvailabilityQuery request, CancellationToken ct)
    {
        // TODO: desk.Bookings not all. only actual or for target date. Need to optimize query
        var desk = await repository.FetchDeskWithBookingsAsync(request.DeskId, request.TargetDate, ct) 
            ?? throw new NotFoundException($"Desk with ID {request.DeskId} not found");
        
        var coworking = desk.Coworking;

        var coworkingTimeZone = TimeZoneInfo.FindSystemTimeZoneById(coworking.TimeZoneId);

        var targetDateLocal = TimeZoneInfo.ConvertTime(request.TargetDate, coworkingTimeZone);

        // TODO: revise manually algorithm
        var allSlots = slotGenerator.GenerateSlots(
            DateOnly.FromDateTime(targetDateLocal.Date),
            coworking.OpenTime,
            coworking.CloseTime,
            coworking.SlotSize,
            coworking.TimeZoneId);

        // TODO: optimize. check only slots that intersect with bookings
        var availableSlots = allSlots.Select(slot => new TimeSlotDto(
            slot.Start,
            slot.End,
            IsAvailable: desk.Bookings.Any(b =>
                slot.Start < b.EndTime && b.StartTime < slot.End) is false // TODO: check condition
        )).ToList();

        return new DeskAvailabilityResponse
        {
            DeskId = request.DeskId,
            AvailableSlots = availableSlots
        };
    }
}
