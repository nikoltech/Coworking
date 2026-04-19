using Coworking.Application.Common.Exceptions;
using Coworking.Application.Common.Interfaces;
using Coworking.Application.Features.Bookings.Queries.GetDeskAvailability.Dtos;
using Coworking.Application.Features.Bookings.Queries.GetDeskAvailability.Responces;
using Coworking.Domain.Services.SlotGenerator;
using Coworking.Domain.Specifications;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Application.Features.Bookings.Queries.GetDeskAvailability;

// TODO: Revise logic! Is this availability per day or per whole time?
internal sealed class GetDeskAvailabilityQueryHandler(
    IAppDbContext context,
    ICoworkingRepository repository,
    ISlotGenerator slotGenerator)
    : IRequestHandler<GetDeskAvailabilityQuery, DeskAvailabilityResponse>
{
    public async Task<DeskAvailabilityResponse> Handle(
    GetDeskAvailabilityQuery request, CancellationToken ct)
    {
        // Шаг 1 — лёгкий запрос: только метаданные коворкинга
        var coworkingInfo = await context.Set<Domain.Entities.Coworking>()
            .AsNoTracking()
            .Where(c => c.Desks.Any(d => d.Id == request.DeskId))
            .Select(c => new { c.TimeZoneId, c.OpenTime, c.CloseTime, c.SlotSize })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Desk with ID {request.DeskId} not found.");

        var coworkingTimeZone = TimeZoneInfo.FindSystemTimeZoneById(coworkingInfo.TimeZoneId);

        // Шаг 2 — конвертируем локальную дату коворкинга в UTC границы
        // TargetDate = локальная дата в таймзоне коворкинга
        // Конвертируем в UTC чтобы корректно фильтровать DateTimeOffset в БД
        var dayStartLocal = request.TargetDate.ToDateTime(TimeOnly.MinValue); // 00:00 локально
        var dayEndLocal = request.TargetDate.AddDays(1).ToDateTime(TimeOnly.MinValue); // 00:00 следующего дня локально

        // Самый надежный и чистый путь для 2026 года:
        var startUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(dayStartLocal, coworkingTimeZone), TimeSpan.Zero);
        var endUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(dayEndLocal, coworkingTimeZone), TimeSpan.Zero);

        // Шаг 3 — тяжёлый запрос: стол + бронирования в UTC диапазоне
        var desk = await repository.FetchDeskWithBookingsAsync(request.DeskId, startUtc, endUtc, ct)
            ?? throw new NotFoundException($"Desk with ID {request.DeskId} not found.");

        // Шаг 4 — генерируем слоты в локальном времени коворкинга
        var allSlots = slotGenerator.GenerateSlots(
            request.TargetDate,
            coworkingInfo.OpenTime,
            coworkingInfo.CloseTime,
            coworkingInfo.SlotSize,
            coworkingInfo.TimeZoneId);

        // Шаг 5 — помечаем занятые слоты
        // Сравниваем DateTimeOffset напрямую — UTC под капотом, таймзона не важна
        var bookedRanges = desk.Bookings
            .Select(b => (b.StartTime, b.EndTime))
            .ToList();

        var slots = allSlots
            .Select(slot => new TimeSlotDto(
                slot.Start,
                slot.End,
                IsAvailable: !bookedRanges.Any(b =>
                    DateRangeOverlap.Check(slot.Start, slot.End, b.StartTime, b.EndTime))))
            .ToList();

        return new DeskAvailabilityResponse
        {
            DeskId = request.DeskId,
            AvailableSlots = slots
        };
    }
}
