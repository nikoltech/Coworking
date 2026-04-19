using Coworking.Application.Common.Enums;
using Coworking.Application.Common.Exceptions;
using Coworking.Application.Common.Interfaces;
using Coworking.Application.Common.Synchronization;
using Coworking.Domain.Entities;
using Coworking.Domain.Policies.Rounding;
using Coworking.Domain.Specifications;
using MediatR;

namespace Coworking.Application.Features.Bookings.Commands.Create;

internal class CreateBookingCommandHandler(
    IAppDbContext dataContext,
    IBookingRepository bookingRepo,
    ICoworkingRepository coworkingRepo,
    IBookingRoundingPolicy roundingPolicy,
    IBookingAccessCoordinator bookingAccessCoordinator)
    : IRequestHandler<CreateBookingCommand, int>
{
    public async Task<int> Handle(CreateBookingCommand request, CancellationToken ct)
    {
        var coworking = await coworkingRepo.GetByDeskIdAsync(request.DeskId, ct)
            ?? throw new NotFoundException($"Coworking or desk by desk {request.DeskId} not found.");

        var (start, end) = LocalizeAndRoundInterval(request, coworking);
        ValidateWithinWorkingHours(start, end, coworking);

        await using var lease =
            await bookingAccessCoordinator.WaitIfOverlappingAsync(
                request.DeskId,
                start,
                end,
                ct);

        // Deadlocks as a guarantee in overlaps. Indexes for boosting + retry policy. 
        await using var transaction =
            await dataContext.BeginTransactionAsync(TransactionIsolationLevel.Serializable, ct);

        try
        {
            // RangeS-S
            var isOccupied = await bookingRepo
                .AnyOverlapAsync(request.DeskId, start, end, ct);

            if (isOccupied)
                throw new ConflictException("Space is already booked for this time.");

            var booking = CreateAndInitializeBooking(request, start, end);

            // RangeS-U (level-up locking)
            await bookingRepo.AddAsync(booking, ct);
            await dataContext.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);

            return booking.Id;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private (DateTimeOffset Start, DateTimeOffset End) LocalizeAndRoundInterval(
        CreateBookingCommand request,
        Domain.Entities.Coworking coworking)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(coworking.TimeZoneId);

        var startLocal = TimeZoneInfo.ConvertTime(request.StartTime, zone);
        var endLocal = TimeZoneInfo.ConvertTime(request.EndTime, zone);

        return roundingPolicy.RoundInterval(startLocal, endLocal, coworking.SlotSize);
    }

    private static Booking CreateAndInitializeBooking(
        CreateBookingCommand request,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        var booking = Booking.Create(request.DeskId, request.UserId, start, end);

        if (request.Metadata?.UserTimeZoneId is not null
            && TimeZoneInfo.FindSystemTimeZoneById(request.Metadata.UserTimeZoneId) is TimeZoneInfo userZone)
            booking.UserTimeZoneId = userZone.Id;

        return booking;
    }

    private static void ValidateWithinWorkingHours(
        DateTimeOffset start,
        DateTimeOffset end,
        Domain.Entities.Coworking coworking)
    {
        BookingSpecifications.ValidateAccessPeriod(start, end, coworking);
    }
}
