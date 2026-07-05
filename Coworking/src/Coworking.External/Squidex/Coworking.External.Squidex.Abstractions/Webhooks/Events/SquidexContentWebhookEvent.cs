using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Abstractions.Webhooks.Events;

/// <summary>
/// Rule → Webhook event for a content change. <see cref="Data"/>/<see cref="DataOld"/> stay raw
/// (schema shape varies per content schema) — deserialize with <see cref="DataAs{T}"/> once you
/// know which schema DTO applies.
/// </summary>
public sealed class SquidexContentWebhookEvent : SquidexWebhookEvent
{
    [JsonPropertyName("schemaId")]
    public NamedId? Schema { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Created, Updated, Deleted, Published, Unpublished or StatusChanged.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }

    /// <summary>Present only on Updated events — the content's data before this change.</summary>
    [JsonPropertyName("dataOld")]
    public JsonElement? DataOld { get; set; }

    /// <summary>Deserializes <see cref="Data"/> into the schema DTO. Null if the field is absent.</summary>
    public T? DataAs<T>() =>
        Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? default
            : Data.Deserialize<T>();

    /// <summary>Deserializes <see cref="DataOld"/> into the schema DTO. Null if the field is absent.</summary>
    public T? DataOldAs<T>() =>
        DataOld is not { ValueKind: JsonValueKind.Object or JsonValueKind.Array } dataOld
            ? default
            : dataOld.Deserialize<T>();
}
