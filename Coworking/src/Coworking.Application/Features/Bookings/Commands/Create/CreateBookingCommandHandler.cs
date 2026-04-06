using Coworking.Application.Common.Enums;
using Coworking.Application.Common.Exceptions;
using Coworking.Application.Common.Interfaces;
using Coworking.Domain.Entities;
using MediatR;

namespace Coworking.Application.Features.Bookings.Commands.Create;

internal class CreateBookingCommandHandler(IDataContext dataContext, IBookingRepository repo) : IRequestHandler<CreateBookingCommand, Guid>
{
    public async Task<Guid> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await dataContext.BeginTransactionAsync(TransactionIsolationLevel.Serializable);

        try
        {
            var isOccupied = await repo.AnyOverlapAsync(request.DeskId, request.StartTime, request.EndTime, cancellationToken);

            if (isOccupied)
                throw new ConflictException("Space is already booked for this time.");

            var booking = Booking.Create(request.DeskId, request.UserId, request.StartTime, request.EndTime, request.TimeZoneId);

            await repo.AddAsync(booking, cancellationToken);
            await dataContext.SaveChangesAsync(cancellationToken);

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
