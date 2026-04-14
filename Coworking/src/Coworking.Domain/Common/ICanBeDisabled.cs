namespace Coworking.Domain.Common;

public interface ICanBeDisabled
{
    public DateTime? DisabledAt { get; set; }
}