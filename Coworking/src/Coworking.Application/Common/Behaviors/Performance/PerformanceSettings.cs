namespace Coworking.Application.Common.Behaviors.Performance;

// can be added through options pattern in the future if needed in Infrastructure layer
public record PerformanceSettings(bool Enabled, int ThresholdMs = 500);
