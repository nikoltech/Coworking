// Models/IvFieldTests.cs
using Coworking.External.Squidex.Abstractions.Models;
using FluentAssertions;

namespace Coworking.External.Squidex.UnitTests.Models;

public sealed class IvFieldTests
{
    [Fact]
    public void ImplicitOperator_ConvertsValue_ToIvField()
    {
        // Act
        IvField<string> field = "test";

        // Assert
        field.Value.Should().Be("test");
    }

    [Fact]
    public void ToString_ReturnsValue_AsString()
    {
        // Arrange
        var field = new IvField<int>(42);

        // Assert
        field.ToString().Should().Be("42");
    }

    [Fact]
    public void ToString_ReturnsNull_WhenValueIsNull()
    {
        // Arrange
        var field = new IvField<string>();

        // Assert
        field.ToString().Should().BeNull();
    }

    [Fact]
    public void Constructor_SetsValue_Correctly()
    {
        // Act
        var field = new IvField<bool>(true);

        // Assert
        field.Value.Should().BeTrue();
    }
}