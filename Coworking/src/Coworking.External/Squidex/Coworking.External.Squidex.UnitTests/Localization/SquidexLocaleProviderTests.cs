using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Localization;
using Coworking.External.Squidex.UnitTests.Helpers;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Coworking.External.Squidex.UnitTests.Localization;

public sealed class SquidexLocaleProviderTests
{
    [Fact]
    public void SupportedLocales_ReturnsFromAppsettings_WhenConfigured()
    {
        // Arrange
        var options = SquidexFakes.DefaultOptions();
        var provider = new SquidexLocaleProvider(SquidexFakes.DefaultOptionsMock(options));

        // Act & Assert
        provider.SupportedLocales.Should().BeEquivalentTo(["uk-UA", "en"]);
    }

    [Fact]
    public void SupportedLocales_ReturnsDefaultLocale_WhenNotConfigured()
    {
        // Arrange
        var options = SquidexFakes.DefaultOptions() with { SupportedLocales = [] };
        var provider = new SquidexLocaleProvider(SquidexFakes.DefaultOptionsMock(options));

        // Act & Assert
        provider.SupportedLocales.Should().BeEquivalentTo(["uk-UA"]);
    }

    [Fact]
    public async Task InitializeAsync_UseAppsettings_WhenConfigured()
    {
        // Arrange
        var options = SquidexFakes.DefaultOptions();
        var provider = new SquidexLocaleProvider(SquidexFakes.DefaultOptionsMock(options));
        var client = Substitute.For<SquidexApiClient>();

        // Act
        await provider.InitializeAsync(client);

        // Assert
        provider.SupportedLocales.Should().BeEquivalentTo(["uk-UA", "en"]);
        await client.DidNotReceive().GetAppLocalesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeAsync_FetchesFromSquidex_WhenAppsettingsEmpty()
    {
        // Arrange
        var options = SquidexFakes.DefaultOptions() with { SupportedLocales = [] };
        var provider = new SquidexLocaleProvider(SquidexFakes.DefaultOptionsMock(options));

        var client = Substitute.For<SquidexApiClient>();
        client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
              .Returns(new[] { "uk-UA", "en", "de" }.ToList() as IReadOnlyList<string>);

        // Act
        await provider.InitializeAsync(client);

        // Assert
        provider.SupportedLocales.Should().BeEquivalentTo(["uk-UA", "en", "de"]);
    }

    [Fact]
    public async Task InitializeAsync_FallsBackToDefault_WhenSquidexUnreachable()
    {
        // Arrange
        var options = SquidexFakes.DefaultOptions() with { SupportedLocales = [] };
        var provider = new SquidexLocaleProvider(SquidexFakes.DefaultOptionsMock(options));

        var client = Substitute.For<SquidexApiClient>();
        client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
              .Throws(new HttpRequestException("Connection refused"));

        // Act
        await provider.InitializeAsync(client);

        // Assert
        provider.SupportedLocales.Should().BeEquivalentTo(["uk-UA"]);
    }

    [Fact]
    public async Task InitializeAsync_IsIdempotent_DoesNotFetchTwice()
    {
        // Arrange
        var options = SquidexFakes.DefaultOptions() with { SupportedLocales = [] };
        var provider = new SquidexLocaleProvider(SquidexFakes.DefaultOptionsMock(options));

        var client = Substitute.For<SquidexApiClient>();
        client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
              .Returns(new[] { "uk-UA" }.ToList() as IReadOnlyList<string>);

        // Act
        await provider.InitializeAsync(client);
        await provider.InitializeAsync(client);

        // Assert — fetched only once
        await client.Received(1).GetAppLocalesAsync(Arg.Any<CancellationToken>());
    }
}