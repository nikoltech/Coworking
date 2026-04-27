namespace Coworking.Application.Behaviors.Performance;

// can be added through options pattern in the future if needed in Infrastructure layer
public record PerformanceSettings
{
    public bool Enabled { get; init; } = true;
    public int ThresholdMs { get; init; } = 500;
}
