namespace Coworking.Domain.Common;

public interface IVersionedEntity
{
    Guid Version { get; set; }
}
