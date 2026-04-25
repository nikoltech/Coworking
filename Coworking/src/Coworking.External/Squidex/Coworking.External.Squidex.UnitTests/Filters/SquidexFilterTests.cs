// Filters/SquidexFilterTests.cs
using Coworking.External.Squidex.Abstractions.Filters;
using Coworking.External.Squidex.UnitTests.Helpers;
using FluentAssertions;

namespace Coworking.External.Squidex.UnitTests.Filters;

public sealed class SquidexFilterTests
{
    [Theory]
    [InlineData("eq")]
    [InlineData("ne")]
    [InlineData("gt")]
    [InlineData("lt")]
    [InlineData("ge")]
    [InlineData("le")]
    public void ComparisonOperator_SetsCorrectOp(string expectedOp)
    {
        // Act
        FilterObject filter = expectedOp switch
        {
            "eq" => SquidexFilter.Eq("data.Name.iv", "test"),
            "ne" => SquidexFilter.Ne("data.Name.iv", "test"),
            "gt" => SquidexFilter.Gt("data.Age.iv", 18),
            "lt" => SquidexFilter.Lt("data.Age.iv", 18),
            "ge" => SquidexFilter.Ge("data.Age.iv", 18),
            "le" => SquidexFilter.Le("data.Age.iv", 18),
            _ => throw new ArgumentOutOfRangeException()
        };

        // Assert
        filter.Op.Should().Be(expectedOp);
        filter.Path.Should().NotBeNullOrEmpty();
        filter.Value.Should().NotBeNull();
    }

    [Fact]
    public void Eq_SetsPathAndValue_Correctly()
    {
        // Act
        var filter = SquidexFilter.Eq("data.Name.iv", "Kyiv");

        // Assert
        filter.Path.Should().Be("data.Name.iv");
        filter.Op.Should().Be("eq");
        filter.Value.Should().Be("Kyiv");
    }

    [Fact]
    public void In_ReturnsFilterObject_WithCollectionValue()
    {
        // Act
        var filter = SquidexFilter.In("data.Id.iv", ["a", "b", "c"]);

        // Assert
        filter.Op.Should().Be("in");
        filter.Value.As<IEnumerable<string>>().Should().BeEquivalentTo(["a", "b", "c"]);
    }

    [Fact]
    public void Contains_SetsCorrectOp()
    {
        // Act
        var filter = SquidexFilter.Contains("data.Title.iv", "city");

        // Assert
        filter.Op.Should().Be("contains");
        filter.Value.Should().Be("city");
    }

    [Fact]
    public void StartsWith_SetsCorrectOp()
    {
        // Act
        var filter = SquidexFilter.StartsWith("data.Title.iv", "Ky");

        // Assert
        filter.Op.Should().Be("startsWith");
    }

    [Fact]
    public void Empty_SetsNullValue()
    {
        // Act
        var filter = SquidexFilter.Empty("data.Description.iv");

        // Assert
        filter.Op.Should().Be("empty");
        filter.Value.Should().BeNull();
    }

    [Fact]
    public void Exists_SetsNullValue()
    {
        // Act
        var filter = SquidexFilter.Exists("data.Title.iv");

        // Assert
        filter.Op.Should().Be("exists");
        filter.Value.Should().BeNull();
    }

    [Fact]
    public void And_ContainsAndKey_WithAllFilters()
    {
        // Arrange
        var f1 = SquidexFilter.Eq("data.Name.iv", "test");
        var f2 = SquidexFilter.Eq("data.Active.iv", true);
        var f3 = SquidexFilter.Gt("data.Age.iv", 18);

        // Act
        var logical = SquidexFilter.And(f1, f2, f3);

        // Assert
        logical.Should().ContainKey("and");
        logical["and"].Should().HaveCount(3);
    }

    [Fact]
    public void Or_ContainsOrKey_WithAllFilters()
    {
        // Arrange
        var f1 = SquidexFilter.Eq("data.Status.iv", TestStatuses.Published);
        var f2 = SquidexFilter.Eq("data.Status.iv", TestStatuses.Draft);

        // Act
        var logical = SquidexFilter.Or(f1, f2);

        // Assert
        logical.Should().ContainKey("or");
        logical["or"].Should().HaveCount(2);
    }

    // ── SquidexPaths ──────────────────────────────────────────────────────────

    [Fact]
    public void SquidexPaths_Iv_BuildsCorrectPath()
    {
        SquidexPaths.Iv("Title").Should().Be("data.Title.iv");
    }

    [Fact]
    public void SquidexPaths_Localized_BuildsCorrectPath()
    {
        SquidexPaths.Localized("Title", TestLocales.UkUA).Should().Be($"data.Title.{TestLocales.UkUA}");
    }

    [Fact]
    public void SquidexPaths_DataRoot_IsData()
    {
        SquidexPaths.DataRoot.Should().Be("data");
    }
}