using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Abstractions.Models;

/// <summary>
/// Squidex invariant (non-localized) field. Wraps {"iv": value}.
/// Use when X-Flatten is not enabled.
/// </summary>
public sealed class IvField<T>
{
    [JsonPropertyName("iv")]
    public T? Value { get; set; }

    public IvField() { }
    public IvField(T value) => Value = value;

    public static implicit operator IvField<T>(T value) => new(value);

    public override string? ToString() => Value?.ToString();
}