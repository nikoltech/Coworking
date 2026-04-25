// Localization/SquidexLocaleProviderTests.cs
using Coworking.External.Squidex.Abstractions.Repository;
using Coworking.External.Squidex.Localization;
using Coworking.External.Squidex.UnitTests.Helpers;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Coworking.External.Squidex.UnitTests.Localization;

public sealed class SquidexLocaleProviderTests
{
    private readonly ISquidexApiClient _client = Substitute.For<ISquidexApiClient>();

    [Fact]
    public void SupportedLocales_ReturnsFromAppsettings_WhenConfigured()
    {
        // Arrange
        var provider = new SquidexLocaleProvider(SquidexFakes.OptionsMock());

        // Assert
        provider.SupportedLocales.Should()
            .BeEquivalentTo([TestLocales.UkUA, TestLocales.En]);
    }

    [Fact]
    public void SupportedLocales_ReturnsDefaultLocale_WhenNoLocalesConfigured()
    {
        // Arrange
        var options = SquidexFakes.DefaultOptions() with { SupportedLocales = [] };
        var provider = new SquidexLocaleProvider(SquidexFakes.OptionsMock(options));

        // Assert
        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA]);
    }

    [Fact]
    public void DefaultLocale_ReturnsValueFromOptions()
    {
        // Arrange
        var provider = new SquidexLocaleProvider(SquidexFakes.OptionsMock());

        // Assert
        provider.DefaultLocale.Should().Be(TestLocales.UkUA);
    }

    [Fact]
    public async Task InitializeAsync_UsesAppsettings_DoesNotCallSquidex()
    {
        // Arrange — SupportedLocales set in appsettings
        var provider = new SquidexLocaleProvider(SquidexFakes.OptionsMock());

        // Act
        await provider.InitializeAsync(_client);

        // Assert — Squidex not called because appsettings wins
        await _client.DidNotReceive().GetAppLocalesAsync(Arg.Any<CancellationToken>());
        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA, TestLocales.En]);
    }

    [Fact]
    public async Task InitializeAsync_FetchesFromSquidex_WhenAppsettingsEmpty()
    {
        // Arrange
        var options = SquidexFakes.DefaultOptions() with { SupportedLocales = [] };
        var provider = new SquidexLocaleProvider(SquidexFakes.OptionsMock(options));

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(new[] { TestLocales.UkUA, TestLocales.En, TestLocales.De }
                   .ToList() as IReadOnlyList<string>);

        // Act
        await provider.InitializeAsync(_client);

        // Assert
        provider.SupportedLocales.Should()
            .BeEquivalentTo([TestLocales.UkUA, TestLocales.En, TestLocales.De]);
    }

    [Fact]
    public async Task InitializeAsync_FallsBackToDefaultLocale_WhenSquidexUnreachable()
    {
        // Arrange
        var options = SquidexFakes.DefaultOptions() with { SupportedLocales = [] };
        var provider = new SquidexLocaleProvider(SquidexFakes.OptionsMock(options));

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Throws(new HttpRequestException("Connection refused"));

        // Act
        await provider.InitializeAsync(_client);

        // Assert — graceful fallback
        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA]);
    }

    [Fact]
    public async Task InitializeAsync_IsIdempotent_CallsSquidexOnlyOnce()
    {
        // Arrange
        var options = SquidexFakes.DefaultOptions() with { SupportedLocales = [] };
        var provider = new SquidexLocaleProvider(SquidexFakes.OptionsMock(options));

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(new[] { TestLocales.UkUA }.ToList() as IReadOnlyList<string>);

        // Act
        await provider.InitializeAsync(_client);
        await provider.InitializeAsync(_client); // second call — should be no-op

        // Assert
        await _client.Received(1).GetAppLocalesAsync(Arg.Any<CancellationToken>());
    }
}