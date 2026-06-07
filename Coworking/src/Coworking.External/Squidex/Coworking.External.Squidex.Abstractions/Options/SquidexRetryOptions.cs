namespace Coworking.External.Squidex.Abstractions.Options;

public sealed record SquidexRetryOptions
{
    public int MaxAttempts { get; init; } = 3;
    public double BaseDelaySeconds { get; init; } = 1.0;
}
