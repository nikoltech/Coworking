using Coworking.External.Squidex.Abstractions.Filters;
using Coworking.External.Squidex.Abstractions.Localization;
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
    public void ComparisonOperators_ReturnCorrectFilterObject(string op)
    {
        // Act
        var filter = op switch
        {
            "eq" => SquidexFilter.Eq("data.Name.iv", "test"),
            "ne" => SquidexFilter.Ne("data.Name.iv", "test"),
            "gt" => SquidexFilter.Gt("data.Age.iv", 18),
            "lt" => SquidexFilter.Lt("data.Age.iv", 18),
            "ge" => SquidexFilter.Ge("data.Age.iv", 18),
            "le" => SquidexFilter.Le("data.Age.iv", 18),
            _ => throw new ArgumentOutOfRangeException(op)
        };

        // Assert
        filter.Op.Should().Be(op);
        filter.Path.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void In_ReturnsFilterObject_WithCorrectValues()
    {
        // Act
        var filter = SquidexFilter.In("data.Id.iv", ["a", "b", "c"]);

        // Assert
        filter.Op.Should().Be("in");
        filter.Value.Should().BeEquivalentTo(new[] { "a", "b", "c" });
    }

    [Fact]
    public void And_ReturnsFilterLogical_WithAndKey()
    {
        // Arrange
        var f1 = SquidexFilter.Eq("data.Name.iv", "test");
        var f2 = SquidexFilter.Eq("data.Active.iv", true);

        // Act
        var logical = SquidexFilter.And(f1, f2);

        // Assert
        logical.Should().ContainKey("and");
        logical["and"].Should().HaveCount(2);
    }

    [Fact]
    public void Or_ReturnsFilterLogical_WithOrKey()
    {
        // Arrange
        var f1 = SquidexFilter.Eq("data.Status.iv", "active");
        var f2 = SquidexFilter.Eq("data.Status.iv", "pending");

        // Act
        var logical = SquidexFilter.Or(f1, f2);

        // Assert
        logical.Should().ContainKey("or");
    }

    [Fact]
    public void Empty_ReturnsFilterObject_WithNullValue()
    {
        // Act
        var filter = SquidexFilter.Empty("data.Description.iv");

        // Assert
        filter.Op.Should().Be("empty");
        filter.Value.Should().BeNull();
    }

    [Fact]
    public void SquidexPaths_Iv_BuildsCorrectPath()
    {
        // Act
        var path = SquidexPaths.Iv("Title");

        // Assert
        path.Should().Be("data.Title.iv");
    }

    [Fact]
    public void SquidexPaths_Localized_BuildsCorrectPath()
    {
        // Act
        var path = SquidexPaths.Localized("Title", SquidexLocales.UkUA);

        // Assert
        path.Should().Be("data.Title.uk-UA");
    }
}