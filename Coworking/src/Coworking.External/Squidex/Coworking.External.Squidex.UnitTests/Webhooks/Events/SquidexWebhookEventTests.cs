using Coworking.External.Squidex.Abstractions.Webhooks.Events;
using Coworking.External.Squidex.UnitTests.Helpers;
using FluentAssertions;
using System.Text.Json;

namespace Coworking.External.Squidex.UnitTests.Webhooks.Events;

public sealed class SquidexWebhookEventTests
{
    private const string ContentEventJson = """
        {
            "appId": { "id": "app-1", "name": "my-app" },
            "timestamp": "2026-07-05T12:00:00Z",
            "actor": { "id": "actor-1", "type": "client" },
            "schemaId": { "id": "schema-1", "name": "cities" },
            "id": "content-1",
            "type": "Updated",
            "status": "Published",
            "data": { "Name": { "iv": "Kyiv" } }
        }
        """;

    private const string AssetEventJson = """
        {
            "appId": { "id": "app-1", "name": "my-app" },
            "timestamp": "2026-07-05T12:00:00Z",
            "actor": { "id": "actor-1", "type": "client" },
            "id": "asset-1",
            "type": "Created",
            "mimeType": "image/png",
            "fileName": "logo.png",
            "fileSize": 1024,
            "pixelWidth": 800,
            "pixelHeight": 600,
            "assetType": "Image"
        }
        """;

    // ── Classification ────────────────────────────────────────────────────────

    [Fact]
    public void Classify_ReturnsContent_WhenSchemaIdPresent()
    {
        var evt = JsonDocument.Parse(ContentEventJson).RootElement;

        SquidexWebhookEventClassifier.Classify(evt).Should().Be(SquidexWebhookEventKind.Content);
    }

    [Fact]
    public void Classify_ReturnsAsset_WhenMimeTypePresent()
    {
        var evt = JsonDocument.Parse(AssetEventJson).RootElement;

        SquidexWebhookEventClassifier.Classify(evt).Should().Be(SquidexWebhookEventKind.Asset);
    }

    [Fact]
    public void Classify_ReturnsUnknown_WhenNeitherFieldPresent()
    {
        var evt = JsonDocument.Parse("{ \"foo\": \"bar\" }").RootElement;

        SquidexWebhookEventClassifier.Classify(evt).Should().Be(SquidexWebhookEventKind.Unknown);
    }

    // ── Content event ─────────────────────────────────────────────────────────

    [Fact]
    public void ContentEvent_DeserializesCommonAndSpecificFields()
    {
        var evt = JsonSerializer.Deserialize<SquidexContentWebhookEvent>(ContentEventJson)!;

        evt.App!.Name.Should().Be("my-app");
        evt.Actor!.Type.Should().Be("client");
        evt.Schema!.Name.Should().Be("cities");
        evt.Id.Should().Be("content-1");
        evt.Type.Should().Be("Updated");
        evt.Status.Should().Be("Published");
    }

    [Fact]
    public void ContentEvent_DataAs_DeserializesSchemaDto()
    {
        var evt = JsonSerializer.Deserialize<SquidexContentWebhookEvent>(ContentEventJson)!;

        var data = evt.DataAs<SquidexFakes.TestSchema>();

        data!.Name!.Value.Should().Be("Kyiv");
    }

    [Fact]
    public void ContentEvent_DataOldAs_ReturnsNull_WhenFieldAbsent()
    {
        var evt = JsonSerializer.Deserialize<SquidexContentWebhookEvent>(ContentEventJson)!;

        evt.DataOldAs<SquidexFakes.TestSchema>().Should().BeNull();
    }

    // ── Asset event ───────────────────────────────────────────────────────────

    [Fact]
    public void AssetEvent_DeserializesAllFields()
    {
        var evt = JsonSerializer.Deserialize<SquidexAssetWebhookEvent>(AssetEventJson)!;

        evt.Id.Should().Be("asset-1");
        evt.Type.Should().Be("Created");
        evt.MimeType.Should().Be("image/png");
        evt.FileName.Should().Be("logo.png");
        evt.FileSize.Should().Be(1024);
        evt.PixelWidth.Should().Be(800);
        evt.PixelHeight.Should().Be(600);
        evt.AssetType.Should().Be("Image");
    }
}
