using Coworking.Application.Abstractions;
using Coworking.Application.Common.Exceptions;
using MediatR;

namespace Coworking.Application.Features.Bookings.Commands.Cancel;

// TODO: fill the missing users  and etc. properties
public record CancelBookingCommand(
    int DeskId,
    Guid UserId,
    int bookingId) : IRequest;

internal class CancelBookingCommandHandler(
    IMediator mediator,
    IAppDbContext dataContext,
    IBookingRepository bookingRepo,
    ICoworkingRepository coworkingRepo)
    : IRequestHandler<CancelBookingCommand>
{
    public async Task Handle(CancelBookingCommand request, CancellationToken ct)
    {
        using var transaction = await dataContext.BeginTransactionAsync(ct);

        try
        {
            var booking = await bookingRepo.GetByIdAsync(request.bookingId, ct)
                ?? throw new NotFoundException($"Booking with ID {request.bookingId} not found.");

            await dataContext.SaveChangesAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        // TODO: fix
        await mediator.Publish(new BookingCancelledNotification(
            UserEmail: default, 
            UserName: default, 
            DeskName: default,
            CoworkingName: default,
            Start: default,
            End: default,
            TimeZoneId: default,
            CancellationReason: default), ct);
    }
}