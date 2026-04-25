// Models/LocalizedFieldTests.cs
using Coworking.External.Squidex.Abstractions.Localization;
using Coworking.External.Squidex.Abstractions.Models;
using FluentAssertions;

namespace Coworking.External.Squidex.UnitTests.Models;

public sealed class LocalizedFieldTests
{
    [Fact]
    public void GetLocalized_ReturnsValue_ForRequestedLocale()
    {
        // Arrange
        var field = new LocalizedField<string>
        {
            [SquidexLocales.UkUA] = "Київ",
            [SquidexLocales.En] = "Kyiv"
        };

        // Act & Assert
        field.GetLocalized(SquidexLocales.En, SquidexLocales.UkUA).Should().Be("Kyiv");
    }

    [Fact]
    public void GetLocalized_FallsBackToDefault_WhenLocaleNotFound()
    {
        // Arrange
        var field = new LocalizedField<string>
        {
            [SquidexLocales.UkUA] = "Київ"
        };

        // Act & Assert
        field.GetLocalized(SquidexLocales.En, SquidexLocales.UkUA).Should().Be("Київ");
    }

    [Fact]
    public void GetLocalized_ReturnsNull_WhenNeitherLocaleExists()
    {
        // Arrange
        var field = new LocalizedField<string>();

        // Act & Assert
        field.GetLocalized(SquidexLocales.En, SquidexLocales.UkUA).Should().BeNull();
    }
}