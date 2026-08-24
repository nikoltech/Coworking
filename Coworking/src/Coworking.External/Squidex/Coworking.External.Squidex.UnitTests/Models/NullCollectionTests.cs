using Coworking.External.Squidex.Abstractions.Models;
using FluentAssertions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.UnitTests.Models;

/// <summary>
/// A plain initializer would survive an absent field but be overwritten by an explicit
/// null, so the models coalesce instead. These pin that down.
/// </summary>
public sealed class NullCollectionTests
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Theory]
    [InlineData("""{"total":0}""")]
    [InlineData("""{"total":0,"items":null}""")]
    public void ResponseSchema_HandsOutAnEmptyList(string json)
    {
        var result = JsonSerializer.Deserialize<ResponseSchema<object>>(json, Json);

        result!.Items.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData("""{"total":0}""")]
    [InlineData("""{"total":0,"items":null}""")]
    public void AssetsResponse_HandsOutAnEmptyList(string json)
    {
        var result = JsonSerializer.Deserialize<AssetsResponse>(json, Json);

        result!.Items.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData("""{"id":"a"}""")]
    [InlineData("""{"id":"a","tags":null}""")]
    public void AssetDto_HandsOutEmptyTags(string json)
    {
        var result = JsonSerializer.Deserialize<AssetDto>(json, Json);

        result!.Tags.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void RealItemsSurvive()
    {
        var result = JsonSerializer.Deserialize<AssetsResponse>(
            """{"total":1,"items":[{"id":"a","tags":["x"]}]}""", Json);

        result!.Items.Should().ContainSingle();
        result.Items[0].Tags.Should().Equal("x");
    }
}
