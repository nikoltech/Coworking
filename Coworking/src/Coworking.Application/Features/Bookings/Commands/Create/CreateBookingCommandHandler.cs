using Coworking.Application.Abstractions;
using Coworking.Application.Abstractions.Synchronization;
using Coworking.Application.Common.Enums;
using Coworking.Application.Common.Exceptions;
using Coworking.Application.Features.Bookings.Commands.Create.Notifications;
using Coworking.Application.Features.Bookings.Commands.Create.Responces;
using Coworking.Domain.Entities;
using Coworking.Domain.Enums;
using Coworking.Domain.Policies.Rounding;
using Coworking.Domain.Specifications;
using MediatR;

namespace Coworking.Application.Features.Bookings.Commands.Create;

internal class CreateBookingCommandHandler(
    IMediator mediator,
    IAppDbContext dataContext,
    IBookingRepository bookingRepo,
    ICoworkingRepository coworkingRepo,
    IBookingRoundingPolicy roundingPolicy,
    IBookingAccessCoordinator bookingAccessCoordinator)
    : IRequestHandler<CreateBookingCommand, CreateBookingCommandResponse>
{
    public async Task<CreateBookingCommandResponse> Handle(
        CreateBookingCommand request,
        CancellationToken ct)
    {
        var desk = await coworkingRepo.GetDeskWithCoworkingAsync(request.DeskId, ct)
            ?? throw new NotFoundException($"Desk with id {request.DeskId} not found.");

        var coworking = desk.Coworking
            ?? throw new NotFoundException($"Coworking for desk {request.DeskId} not found.");

        var (start, end) = LocalizeAndRoundInterval(request, coworking);
        ValidateWithinWorkingHours(start, end, coworking);

        await using var lease =
            await bookingAccessCoordinator.WaitIfOverlappingAsync(
                request.DeskId,
                start,
                end,
                ct);

        Booking? booking;

        // Deadlocks as a guarantee in overlaps (MSSQL) or SSI conflict at commit (PostgreSQL). Indexes for boosting + retry policy.
        await using var transaction =
            await dataContext.BeginTransactionAsync(TransactionIsolationLevel.Serializable, ct);

        try
        {
            // RangeS-S
            var isOccupied = await bookingRepo
                .AnyOverlapAsync(request.DeskId, start, end, ct);

            if (isOccupied)
                throw new ConflictException("Space is already booked for this time.");

            booking = CreateAndInitializeBooking(request, start, end);

            // RangeS-U (level-up locking)
            await bookingRepo.AddAsync(booking, ct);
            await PublishBookingCreatedAsync(request, desk, start, end, ct);
            await dataContext.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        return new(booking.AccessCode, booking.Id);
    }

    private Task PublishBookingCreatedAsync(CreateBookingCommand request,
        Desk desk,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct)
    {
        return mediator.Publish(new BookingCreatedNotification(
            UserEmail: request.UserEmail,
            UserName: request.UserName,
            DeskName: desk.Name,
            CoworkingName: desk.Coworking.Name,
            Start: start,
            End: end,
            TimeZoneId: desk.Coworking.TimeZoneId), ct);
    }


    private (DateTimeOffset Start, DateTimeOffset End) LocalizeAndRoundInterval(
        CreateBookingCommand request,
        Domain.Entities.Coworking coworking)
    {
        // Note: think about user time zone. Now it supposing that user books time in coworking local time zone.
        var zone = TimeZoneInfo.FindSystemTimeZoneById(coworking.TimeZoneId);

        var startLocal = TimeZoneInfo.ConvertTime(request.StartTime, zone);
        var endLocal = TimeZoneInfo.ConvertTime(request.EndTime, zone);

        return roundingPolicy.RoundInterval(startLocal, endLocal, coworking.SlotSize);
    }

    private static Booking CreateAndInitializeBooking(CreateBookingCommand request,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        var booking = Booking.Create(request.DeskId, request.UserName, request.UserEmail, start, end);

        booking.SetStatus(BookingStatus.PendingPayment);

        if (request.Metadata?.UserTimeZoneId is not null
            && TimeZoneInfo.FindSystemTimeZoneById(request.Metadata.UserTimeZoneId) is TimeZoneInfo userZone)
            booking.UserTimeZoneId = userZone.Id;

        return booking;
    }

    private static void ValidateWithinWorkingHours(DateTimeOffset start,
        DateTimeOffset end,
        Domain.Entities.Coworking coworking)
    {
        BookingSpecifications.ValidateAccessPeriod(start, end, coworking);
    }
}
