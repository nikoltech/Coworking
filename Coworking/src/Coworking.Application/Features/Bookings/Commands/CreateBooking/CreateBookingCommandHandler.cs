using Coworking.Application.Common.Enums;
using Coworking.Application.Common.Interfaces;
using Coworking.Domain.Entities;
using MediatR;

namespace Coworking.Application.Features.Bookings.Commands.CreateBooking;

// TODO
internal class CreateBookingCommandHandler(IUnitOfWork _dbContext) : IRequestHandler<CreateBookingCommand, Guid>
{
    public async Task<Guid> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await _dbContext.BeginTransactionAsync(TransactionIsolationLevel.Serializable); // with auto Range Locks

        try
        {
            var isOccupied = await _bookingRepository.AnyOverlapAsync(
                request.DeskId, request.StartTime, request.EndTime, cancellationToken);

            if (isOccupied)
                throw new ConflictException("Space is already booked for this time.");

            var booking = Booking.Create(request.DeskId, request.UserId, request.StartTime, request.EndTime, request.TimeZoneId);

            await _bookingRepository.AddAsync(booking, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return booking.Id;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
