using Coworking.Domain.Common;

namespace Coworking.Domain.Entities;

public class Desk : ITrackEntity, ICanBeDisabled
{
    public int Id { get; private set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public int CoworkingId { get; set; }

    public required Coworking Coworking { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DisabledAt { get; set; }

    public ICollection<Booking> Bookings { get; set; } = [];
}
