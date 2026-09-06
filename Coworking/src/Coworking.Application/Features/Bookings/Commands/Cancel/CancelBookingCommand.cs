using Coworking.Application.Ports;
using Coworking.Application.Common.Exceptions;
using Coworking.Application.Features.Bookings.Commands.Cancel.Notifications;
using Coworking.Domain.Entities;
using Coworking.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Application.Features.Bookings.Commands.Cancel;

public record CancelBookingCommand(long BookingId, Guid AccessCode) : IRequest;

internal class CancelBookingCommandHandler(IMediator mediator, IAppDbContext dataContext) : IRequestHandler<CancelBookingCommand>
{
    public async Task Handle(CancelBookingCommand request, CancellationToken ct)
    {
        await using var transaction = await dataContext.BeginTransactionAsync(ct);

        var booking = await dataContext.Set<Booking>()
            .Include(b => b.Desk)
                .ThenInclude(d => d.Coworking)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, ct);

        // a wrong code is answered exactly like a missing booking, so neither reveals the other
        if (booking is null || booking.AccessCode != request.AccessCode)
            throw new NotFoundException($"Booking {request.BookingId} not found.");

        booking.Cancel();

        await PublishBookingCancelledAsync(booking, ct);

        try
        {
            await dataContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictException(
                $"Booking {request.BookingId} was modified concurrently.", ex);
        }

        await transaction.CommitAsync();
    }

    private Task PublishBookingCancelledAsync(Booking booking, CancellationToken ct) =>
        mediator.Publish(new BookingCancelledNotification(
            UserEmail: booking.UserEmail,
            UserName: booking.UserName,
            DeskName: booking.Desk.Name,
            CoworkingName: booking.Desk.Coworking.Name,
            Start: booking.StartTime,
            End: booking.EndTime,
            TimeZoneId: booking.Desk.Coworking.TimeZoneId,
            CancellationReason: CancellationReasons.ByUser), ct);
}
