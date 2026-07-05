using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Abstractions.Webhooks.Events;

/// <summary>
/// Common fields on every Squidex Rule → Webhook event payload (content or asset).
/// </summary>
public abstract class SquidexWebhookEvent
{
    [JsonPropertyName("appId")]
    public NamedId? App { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("actor")]
    public WebhookActor? Actor { get; set; }
}

public sealed record NamedId(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name);

public sealed record WebhookActor(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type);
