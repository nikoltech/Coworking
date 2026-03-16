using Coworking.Application.Common.Interfaces;
using Coworking.Domain.Entities;
using MediatR;

namespace Coworking.Application.Features.Bookings.Commands;

// generated. TODO
internal class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Guid>
{
    private readonly IDbContext _dbContext;

    public CreateBookingCommandHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = new Booking
        {
            DeskId = request.DeskId,
            UserId = request.UserId,
            Start = request.Start,
            End = request.End
        };

        _dbContext.Set<Booking>().Add(booking);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return booking.Id;
    }
}
