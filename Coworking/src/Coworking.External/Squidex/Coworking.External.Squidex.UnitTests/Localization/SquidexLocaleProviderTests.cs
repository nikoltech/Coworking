// Localization/SquidexLocaleProviderTests.cs
using Coworking.External.Squidex.Abstractions.Localization;
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Client;
using Coworking.External.Squidex.Localization;
using Coworking.External.Squidex.UnitTests.Helpers;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Coworking.External.Squidex.UnitTests.Localization;

public sealed class SquidexLocaleProviderTests
{
    private readonly ISquidexApiClient _client = Substitute.For<ISquidexApiClient>();

    // ── Before initialization ────────────────────────────────────────────────

    [Fact]
    public void SupportedLocales_ReturnsFromAppsettings_WhenConfigured()
    {
        var provider = new SquidexLocaleProvider(SquidexFakes.DefaultAppOptions());

        provider.SupportedLocales.Should()
            .BeEquivalentTo([TestLocales.UkUA, TestLocales.En]);
    }

    [Fact]
    public void SupportedLocales_ReturnsDefaultLocale_WhenNoLocalesConfigured()
    {
        var options = SquidexFakes.AppOptionsWithoutLocales();
        var provider = new SquidexLocaleProvider(options);

        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA]);
    }

    [Fact]
    public void DefaultLocale_ReturnsValueFromOptions_BeforeInitialize()
    {
        var provider = new SquidexLocaleProvider(SquidexFakes.DefaultAppOptions());

        provider.DefaultLocale.Should().Be(TestLocales.UkUA);
    }

    // ── After initialization — appsettings set ────────────────────────────────

    [Fact]
    public async Task InitializeAsync_UsesAppsettings_DoesNotCallSquidex()
    {
        // Arrange — SupportedLocales configured in appsettings
        var provider = new SquidexLocaleProvider(SquidexFakes.DefaultAppOptions());

        // Act
        await provider.InitializeAsync(_client);

        // Assert — Squidex not called because appsettings wins
        await _client.DidNotReceive().GetAppLocalesAsync(Arg.Any<CancellationToken>());
        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA, TestLocales.En]);
    }

    [Fact]
    public async Task InitializeAsync_KeepsDefaultLocaleFromAppsettings_WhenLocalesConfigured()
    {
        // Arrange — DefaultLocale explicitly set + SupportedLocales set
        var options = SquidexFakes.DefaultAppOptions() with { DefaultLocale = TestLocales.En };
        var provider = new SquidexLocaleProvider(options);

        // Act
        await provider.InitializeAsync(_client);

        // Assert — DefaultLocale stays from appsettings, Squidex not called
        provider.DefaultLocale.Should().Be(TestLocales.En);
        await _client.DidNotReceive().GetAppLocalesAsync(Arg.Any<CancellationToken>());
    }

    // ── After initialization — appsettings empty, fetches from Squidex ───────

    [Fact]
    public async Task InitializeAsync_FetchesFromSquidex_WhenSupportedLocalesNotConfigured()
    {
        // Arrange
        var options = SquidexFakes.AppOptionsWithoutLocales();
        var provider = new SquidexLocaleProvider(options);

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeLocales(TestLocales.UkUA, TestLocales.En, TestLocales.De));

        // Act
        await provider.InitializeAsync(_client);

        // Assert
        provider.SupportedLocales.Should()
            .BeEquivalentTo([TestLocales.UkUA, TestLocales.En, TestLocales.De]);
    }

    [Fact]
    public async Task InitializeAsync_SetsDefaultLocale_FromIsMasterField()
    {
        // Arrange — appsettings DefaultLocale left at the library default ("en"),
        // which SquidexLocaleProvider treats as "not explicitly configured".
        // Squidex says "uk-UA" is master
        var options = SquidexFakes.AppOptionsWithoutLocales() with { DefaultLocale = SquidexLocales.Default };
        var provider = new SquidexLocaleProvider(options);

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeLocales(
                   masterLocale: TestLocales.UkUA,
                   TestLocales.En));

        // Act
        await provider.InitializeAsync(_client);

        // Assert — DefaultLocale set from IsMaster
        provider.DefaultLocale.Should().Be(TestLocales.UkUA);
    }

    [Fact]
    public async Task InitializeAsync_KeepsDefaultLocaleFromAppsettings_WhenExplicitlySet()
    {
        // Arrange — DefaultLocale explicitly set to "de" in appsettings (not the library default "en")
        // Squidex says "uk-UA" is master — appsettings wins
        var options = SquidexFakes.AppOptionsWithoutLocales() with { DefaultLocale = TestLocales.De };
        var provider = new SquidexLocaleProvider(options);

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeLocales(
                   masterLocale: TestLocales.UkUA,
                   TestLocales.En));

        // Act
        await provider.InitializeAsync(_client);

        // Assert — appsettings DefaultLocale wins over IsMaster
        provider.DefaultLocale.Should().Be(TestLocales.De);
    }

    [Fact]
    public async Task InitializeAsync_FallsBackToDefaultLocale_WhenSquidexUnreachable()
    {
        // Arrange
        var options = SquidexFakes.AppOptionsWithoutLocales();
        var provider = new SquidexLocaleProvider(options);

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Throws(new HttpRequestException("Connection refused"));

        // Act
        await provider.InitializeAsync(_client);

        // Assert — graceful fallback
        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA]);
        provider.DefaultLocale.Should().Be(TestLocales.UkUA);
    }

    [Fact]
    public async Task InitializeAsync_FallsBackToDefaultLocale_WhenSquidexReturnsEmpty()
    {
        // Arrange
        var options = SquidexFakes.AppOptionsWithoutLocales();
        var provider = new SquidexLocaleProvider(options);

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(new List<SquidexLocaleInfo>() as IReadOnlyList<SquidexLocaleInfo>);

        // Act
        await provider.InitializeAsync(_client);

        // Assert
        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA]);
    }

    [Fact]
    public async Task InitializeAsync_IsIdempotent_CallsSquidexOnlyOnce()
    {
        // Arrange
        var options = SquidexFakes.AppOptionsWithoutLocales();
        var provider = new SquidexLocaleProvider(options);

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeLocales(TestLocales.UkUA, TestLocales.En));

        // Act
        await provider.InitializeAsync(_client);
        await provider.InitializeAsync(_client); // second call — no-op

        // Assert — fetched only once
        await _client.Received(1).GetAppLocalesAsync(Arg.Any<CancellationToken>());
    }
}