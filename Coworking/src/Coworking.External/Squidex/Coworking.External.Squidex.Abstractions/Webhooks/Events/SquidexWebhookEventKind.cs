using System.Text.Json;

namespace Coworking.External.Squidex.Abstractions.Webhooks.Events;

public enum SquidexWebhookEventKind
{
    Unknown,
    Content,
    Asset,
}

/// <summary>
/// Classifies a raw webhook payload before committing to a typed shape — lets a single
/// endpoint dispatch to the right handling however the consuming app prefers (manual switch,
/// MediatR, a message broker, ...).
/// </summary>
public static class SquidexWebhookEventClassifier
{
    public static SquidexWebhookEventKind Classify(JsonElement evt)
    {
        if (evt.TryGetProperty("schemaId", out _))
            return SquidexWebhookEventKind.Content;

        if (evt.TryGetProperty("mimeType", out _))
            return SquidexWebhookEventKind.Asset;

        return SquidexWebhookEventKind.Unknown;
    }
}
