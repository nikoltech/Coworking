using Coworking.Application.Common.Enums;
using Coworking.Application.Common.Exceptions;
using Coworking.Application.Common.Interfaces;
using Coworking.Application.Common.Synchronization;
using Coworking.Domain.Entities;
using MediatR;

namespace Coworking.Application.Features.Bookings.Commands.Create;

internal class CreateBookingCommandHandler(IDataContext dataContext, IBookingRepository repo,
    IBookingSynchronizer synchronizer)
    : IRequestHandler<CreateBookingCommand, Guid>
{
    public async Task<Guid> Handle(CreateBookingCommand request, CancellationToken ct)
    {
        await using var handle = await synchronizer
            .AcquireAsync(request.DeskId, request.StartTime, request.EndTime, ct);

        // Deadlocks as guarantee in overlaps. Indexes for boosting + retry policy. 
        using var transaction = await dataContext.BeginTransactionAsync(
            TransactionIsolationLevel.Serializable, ct);

        try
        {
            // RangeS-S
            var isOccupied = await repo
                .AnyOverlapAsync(request.DeskId, request.StartTime, request.EndTime, ct);

            if (isOccupied)
                throw new ConflictException("Space is already booked for this time.");

            Booking booking = Booking
                .Create(request.DeskId, request.UserId, request.StartTime, request.EndTime, request.TimeZoneId);

            // RangeS-U
            await repo.AddAsync(booking, ct);

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
}
