namespace Coworking.Domain.Common;

public interface ITrackEntity
{
    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
