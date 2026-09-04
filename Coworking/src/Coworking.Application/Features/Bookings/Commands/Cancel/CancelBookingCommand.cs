using Coworking.Application.Ports;
using Coworking.Application.Common.Exceptions;
using Coworking.Application.Features.Bookings.Commands.Cancel.Notifications;
using Coworking.Domain.Entities;
using Coworking.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Application.Features.Bookings.Commands.Cancel;

public record CancelBookingCommand(
    //Guid UserId,
    Guid AccessCode) : IRequest;

internal class CancelBookingCommandHandler(IMediator mediator, IAppDbContext dataContext) : IRequestHandler<CancelBookingCommand>
{
    public async Task Handle(CancelBookingCommand request, CancellationToken ct)
    {
        Booking booking;

        await using var transaction = await dataContext.BeginTransactionAsync(ct);

        booking = await dataContext.Set<Booking>()
            .Include(b => b.Desk)
                .ThenInclude(d => d.Coworking)
            .FirstOrDefaultAsync(b => b.AccessCode == request.AccessCode, ct)
            ?? throw new NotFoundException($"Booking with access code {request.AccessCode} not found.");

        booking.Cancel();

        await PublishBookingCancelledAsync(booking, ct);

        try
        {
            await dataContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictException(
                $"Booking with access code {request.AccessCode} was modified concurrently.", ex);
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
