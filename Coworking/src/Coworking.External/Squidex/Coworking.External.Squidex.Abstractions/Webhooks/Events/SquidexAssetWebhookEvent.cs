using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Abstractions.Webhooks.Events;

/// <summary>
/// Rule → Webhook event for an asset change.
/// </summary>
public sealed class SquidexAssetWebhookEvent : SquidexWebhookEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Created, Updated, Deleted or Annotated.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    [JsonPropertyName("pixelWidth")]
    public int? PixelWidth { get; set; }

    [JsonPropertyName("pixelHeight")]
    public int? PixelHeight { get; set; }

    [JsonPropertyName("assetType")]
    public string? AssetType { get; set; }
}
