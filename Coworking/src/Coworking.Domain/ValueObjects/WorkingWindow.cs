namespace Coworking.Domain.ValueObjects;

/// <summary>
/// WorkingWindow is one uninterrupted open period, from opening to closing, as real moments.
/// </summary>
public readonly record struct WorkingWindow(DateTimeOffset Start, DateTimeOffset End);
