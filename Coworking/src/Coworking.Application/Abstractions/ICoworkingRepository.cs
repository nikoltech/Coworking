using Coworking.Domain.Entities;
using System.Linq.Expressions;

namespace Coworking.Application.Abstractions;

public interface ICoworkingRepository
{
    Task<Desk?> FetchDeskWithBookingsAsync(
        int deskId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken ct = default);
    Task<Desk> GetDeskWithCoworkingAsync(int deskId, CancellationToken cancellationToken = default);
    Task<List<Domain.Entities.Coworking>> ListAsync(
        Expression<Func<Domain.Entities.Coworking, bool>>? predicate = null,
        CancellationToken ct = default);
    Task<List<Desk>> ListDesksAsync(int coworkingId,
        Expression<Func<Desk, bool>>? predicate = null,
        CancellationToken ct = default);
}
