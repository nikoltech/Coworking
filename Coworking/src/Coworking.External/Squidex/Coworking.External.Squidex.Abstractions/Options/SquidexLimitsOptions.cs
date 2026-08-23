namespace Coworking.External.Squidex.Abstractions.Options;

public sealed record SquidexLimitsOptions
{
    /// <summary>
    /// Caps how many requests a single library operation fans out into — paging a schema,
    /// or splitting an id list into batches. Does not limit how many operations the caller
    /// starts: total load stays the caller's to manage.
    /// </summary>
    public int MaxParallelRequests { get; init; } = 16;
}
