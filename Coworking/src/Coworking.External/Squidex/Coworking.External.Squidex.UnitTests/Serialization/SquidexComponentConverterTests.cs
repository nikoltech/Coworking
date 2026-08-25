using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Serialization;
using FluentAssertions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.UnitTests.Serialization;

public sealed class SquidexComponentConverterTests
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [SquidexComponent("componentType")]
    [SquidexComponentType("hero", typeof(HeroBlock))]
    [SquidexComponentType("cta", typeof(CtaBlock))]
    public abstract class PageBlock
    {
        [JsonPropertyName("schemaId")] public string? SchemaId { get; set; }
    }

    public sealed class HeroBlock : PageBlock
    {
        [JsonPropertyName("heading")] public string? Heading { get; set; }
    }

    public sealed class CtaBlock : PageBlock
    {
        [JsonPropertyName("label")] public string? Label { get; set; }
    }

    public sealed class PageSchema
    {
        [JsonPropertyName("Blocks")] public IvField<List<PageBlock>>? Blocks { get; set; }
    }

    // Squidex puts schemaId first, so the discriminator is not the first property
    private const string DiscriminatorLast = """
    {
      "Blocks": {
        "iv": [
          { "schemaId": "guid-1", "componentType": "hero", "heading": "About" },
          { "schemaId": "guid-2", "componentType": "cta",  "label": "Buy" }
        ]
      }
    }
    """;

    [Fact]
    public void ReadsComponents_WhenDiscriminatorIsNotFirst()
    {
        var page = JsonSerializer.Deserialize<PageSchema>(DiscriminatorLast, Json);

        var blocks = page!.Blocks!.Value!;

        blocks.Should().HaveCount(2);
        blocks[0].Should().BeOfType<HeroBlock>().Which.Heading.Should().Be("About");
        blocks[1].Should().BeOfType<CtaBlock>().Which.Label.Should().Be("Buy");
    }

    [Fact]
    public void WritesDiscriminatorBack()
    {
        var page = new PageSchema
        {
            Blocks = new IvField<List<PageBlock>>(
                [new HeroBlock { SchemaId = "guid-1", Heading = "About" }])
        };

        var json = JsonSerializer.Serialize(page, Json);

        json.Should().Contain("\"componentType\":\"hero\"");
        json.Should().Contain("\"schemaId\":\"guid-1\"");
    }

    [Fact]
    public void FailsClearly_WhenTheDiscriminatorIsMissing()
    {
        const string json = """{ "Blocks": { "iv": [ { "schemaId": "g" } ] } }""";

        var act = () => JsonSerializer.Deserialize<PageSchema>(json, Json);

        act.Should().Throw<JsonException>().WithMessage("*componentType*");
    }

    [Fact]
    public void FailsClearly_WhenTheComponentIsUnknown()
    {
        const string json = """{ "Blocks": { "iv": [ { "componentType": "banner" } ] } }""";

        var act = () => JsonSerializer.Deserialize<PageSchema>(json, Json);

        act.Should().Throw<JsonException>().WithMessage("*banner*");
    }
}
