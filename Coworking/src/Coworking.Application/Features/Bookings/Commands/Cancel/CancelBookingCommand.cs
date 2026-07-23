using Coworking.Application.Abstractions;
using Coworking.Application.Common.Exceptions;
using Coworking.Application.Features.Bookings.Commands.Cancel.Notifications;
using Coworking.Domain.Entities;
using Coworking.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Application.Features.Bookings.Commands.Cancel;

public record CancelBookingCommand(
    //Guid UserId,
    int BookingId) : IRequest;

internal class CancelBookingCommandHandler(IMediator mediator, IAppDbContext dataContext) : IRequestHandler<CancelBookingCommand>
{
    public async Task Handle(CancelBookingCommand request, CancellationToken ct)
    {
        Booking booking;

        using var transaction = await dataContext.BeginTransactionAsync(ct);

        try
        {
            booking = await dataContext.Set<Booking>()
                .Include(b => b.Desk)
                    .ThenInclude(d => d.Coworking)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId, ct)
                ?? throw new NotFoundException($"Booking with ID {request.BookingId} not found.");

            booking.SetStatus(BookingStatus.Cancelled);

            await PublishBookingCancelledAsync(booking, ct);
            await dataContext.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
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
            CancellationReason: default), ct);
}
