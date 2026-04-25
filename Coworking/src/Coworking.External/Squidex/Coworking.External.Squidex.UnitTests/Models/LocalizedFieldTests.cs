// Models/LocalizedFieldTests.cs
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.UnitTests.Helpers;
using FluentAssertions;

namespace Coworking.External.Squidex.UnitTests.Models;

public sealed class LocalizedFieldTests
{
    [Fact]
    public void GetLocalized_ReturnsValueForRequestedLocale()
    {
        // Arrange
        var field = new LocalizedField<string>
        {
            [TestLocales.UkUA] = "Київ",
            [TestLocales.En] = "Kyiv"
        };

        // Assert
        field.GetLocalized(TestLocales.En, TestLocales.UkUA).Should().Be("Kyiv");
    }

    [Fact]
    public void GetLocalized_FallsBackToDefault_WhenLocaleNotPresent()
    {
        // Arrange
        var field = new LocalizedField<string>
        {
            [TestLocales.UkUA] = "Київ"
        };

        // Assert — "en" not present → fallback to "uk-UA"
        field.GetLocalized(TestLocales.En, TestLocales.UkUA).Should().Be("Київ");
    }

    [Fact]
    public void GetLocalized_ReturnsNull_WhenNeitherLocalePresent()
    {
        // Arrange
        var field = new LocalizedField<string>();

        // Assert
        field.GetLocalized(TestLocales.En, TestLocales.UkUA).Should().BeNull();
    }

    [Fact]
    public void Get_ReturnsValue_ForExistingLocale()
    {
        // Arrange
        var field = new LocalizedField<string> { [TestLocales.UkUA] = "Київ" };

        // Assert
        field.Get(TestLocales.UkUA).Should().Be("Київ");
    }

    [Fact]
    public void Get_ReturnsDefault_ForMissingLocale()
    {
        // Arrange
        var field = new LocalizedField<string>();

        // Assert
        field.Get(TestLocales.UkUA).Should().BeNull();
    }
}