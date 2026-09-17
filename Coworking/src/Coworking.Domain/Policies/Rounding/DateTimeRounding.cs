namespace Coworking.Domain.Policies.Rounding;

/// <summary>
/// Rounds to a grid of equal real-time steps from an anchor, so the result does not depend
/// on UTC alignment or on DST changes after the anchor. The result keeps the anchor's offset.
/// </summary>
public static class DateTimeRounding
{
    public static DateTimeOffset FloorToGrid(DateTimeOffset value, DateTimeOffset anchor, TimeSpan step)
    {
        var steps = Math.DivRem((value - anchor).Ticks, step.Ticks, out var remainder);

        if (remainder < 0)
            steps--;

        return anchor + TimeSpan.FromTicks(steps * step.Ticks);
    }

    public static DateTimeOffset CeilToGrid(DateTimeOffset value, DateTimeOffset anchor, TimeSpan step)
    {
        var steps = Math.DivRem((value - anchor).Ticks, step.Ticks, out var remainder);

        if (remainder > 0)
            steps++;

        return anchor + TimeSpan.FromTicks(steps * step.Ticks);
    }
}
