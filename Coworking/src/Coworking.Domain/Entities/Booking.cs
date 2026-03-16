namespace Coworking.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; }

    public Guid DeskId { get; set; }

    public Guid UserId { get; set; }

    public DateTime Start { get; set; }

    public DateTime End { get; set; }
}
