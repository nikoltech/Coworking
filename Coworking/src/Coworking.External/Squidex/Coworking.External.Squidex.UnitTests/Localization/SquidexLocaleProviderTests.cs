// Localization/SquidexLocaleProviderTests.cs
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Client;
using Coworking.External.Squidex.Localization;
using Coworking.External.Squidex.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
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
        var provider = new SquidexLocaleProvider(SquidexFakes.DefaultAppOptions(), NullLogger<SquidexLocaleProvider>.Instance);

        provider.SupportedLocales.Should()
            .BeEquivalentTo([TestLocales.UkUA, TestLocales.En]);
    }

    [Fact]
    public void SupportedLocales_ReturnsDefaultLocale_WhenNoLocalesConfigured()
    {
        var options = SquidexFakes.AppOptionsWithoutLocales();
        var provider = new SquidexLocaleProvider(options, NullLogger<SquidexLocaleProvider>.Instance);

        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA]);
    }

    [Fact]
    public void DefaultLocale_ReturnsValueFromOptions_BeforeInitialize()
    {
        var provider = new SquidexLocaleProvider(SquidexFakes.DefaultAppOptions(), NullLogger<SquidexLocaleProvider>.Instance);

        provider.DefaultLocale.Should().Be(TestLocales.UkUA);
    }

    // ── After initialization — appsettings set ────────────────────────────────

    [Fact]
    public async Task InitializeAsync_UsesAppsettings_DoesNotCallSquidex()
    {
        // Arrange — SupportedLocales configured in appsettings
        var provider = new SquidexLocaleProvider(SquidexFakes.DefaultAppOptions(), NullLogger<SquidexLocaleProvider>.Instance);

        // Act
        await provider.InitializeAsync(_client);

        // Assert — Squidex not called because appsettings wins
        await _client.DidNotReceive().GetAppLocalesAsync(Arg.Any<CancellationToken>());
        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA, TestLocales.En]);
    }

    [Fact]
    public async Task InitializeAsync_KeepsDefaultLocaleFromAppsettings_WhenLocalesConfigured()
    {
        // Arrange — DefaultLocale explicitly set (non-"en", so unambiguously explicit) + SupportedLocales set
        var options = SquidexFakes.DefaultAppOptions() with { DefaultLocale = TestLocales.De };
        var provider = new SquidexLocaleProvider(options, NullLogger<SquidexLocaleProvider>.Instance);

        // Act
        await provider.InitializeAsync(_client);

        // Assert — DefaultLocale stays from appsettings, Squidex not called (both configured)
        provider.DefaultLocale.Should().Be(TestLocales.De);
        await _client.DidNotReceive().GetAppLocalesAsync(Arg.Any<CancellationToken>());
    }

    // ── After initialization — appsettings empty, fetches from Squidex ───────

    [Fact]
    public async Task InitializeAsync_FetchesFromSquidex_WhenSupportedLocalesNotConfigured()
    {
        // Arrange
        var options = SquidexFakes.AppOptionsWithoutLocales();
        var provider = new SquidexLocaleProvider(options, NullLogger<SquidexLocaleProvider>.Instance);

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
        // Arrange — DefaultLocale not configured. Squidex says "uk-UA" is master
        var options = SquidexFakes.AppOptionsWithoutLocales() with { DefaultLocale = string.Empty };
        var provider = new SquidexLocaleProvider(options, NullLogger<SquidexLocaleProvider>.Instance);

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
    public async Task InitializeAsync_Throws_WhenExplicitDefaultLocaleDisagreesWithSquidexMaster()
    {
        // Arrange — DefaultLocale explicitly set to "de", SupportedLocales not configured,
        // so a fetch still happens to fill it in. Squidex says "uk-UA" is master —
        // this is a configuration error, not a transient failure.
        var options = SquidexFakes.AppOptionsWithoutLocales() with { DefaultLocale = TestLocales.De };
        var provider = new SquidexLocaleProvider(options, NullLogger<SquidexLocaleProvider>.Instance);

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeLocales(
                   masterLocale: TestLocales.UkUA,
                   TestLocales.En));

        // Act
        var act = () => provider.InitializeAsync(_client);

        // Assert — mismatch between configured DefaultLocale and Squidex's actual master throws
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*de*uk-UA*");
    }

    [Fact]
    public async Task InitializeAsync_FillsInDefaultLocale_WhenOnlySupportedLocalesConfigured()
    {
        // Arrange — SupportedLocales explicit, DefaultLocale not configured. Squidex says "uk-UA" is master.
        var options = SquidexFakes.DefaultAppOptions() with { DefaultLocale = string.Empty };
        var provider = new SquidexLocaleProvider(options, NullLogger<SquidexLocaleProvider>.Instance);

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeLocales(
                   masterLocale: TestLocales.UkUA,
                   TestLocales.En));

        // Act
        await provider.InitializeAsync(_client);

        // Assert — DefaultLocale filled in from IsMaster, SupportedLocales stays as configured
        provider.DefaultLocale.Should().Be(TestLocales.UkUA);
        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA, TestLocales.En]);
    }

    [Fact]
    public void SupportedLocales_AlwaysIncludesDefaultLocale_AfterNormalization()
    {
        // Arrange — DefaultLocale explicit but not part of the explicitly configured SupportedLocales
        var options = SquidexFakes.DefaultAppOptions() with
        {
            DefaultLocale = TestLocales.De,
            SupportedLocales = [TestLocales.UkUA, TestLocales.En],
        };

        // Act
        var provider = new SquidexLocaleProvider(options, NullLogger<SquidexLocaleProvider>.Instance);

        // Assert — DefaultLocale is unioned in, no duplicates
        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA, TestLocales.En, TestLocales.De]);
    }

    [Fact]
    public void DefaultLocale_Throws_WhenNotConfiguredAndNotYetInitialized()
    {
        // Arrange — neither DefaultLocale nor SupportedLocales explicitly configured
        var options = SquidexFakes.AppOptionsWithoutLocales() with { DefaultLocale = string.Empty };
        var provider = new SquidexLocaleProvider(options, NullLogger<SquidexLocaleProvider>.Instance);

        // Act
        var act = () => provider.DefaultLocale;

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task InitializeAsync_BecomesUnresolved_WhenSquidexUnreachable()
    {
        // Arrange — Squidex is the only guarantor; a failed fetch is never papered over
        // with the appsettings value, even though DefaultLocale was explicit here.
        var options = SquidexFakes.AppOptionsWithoutLocales();
        var provider = new SquidexLocaleProvider(options, NullLogger<SquidexLocaleProvider>.Instance);

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Throws(new HttpRequestException("Connection refused"));

        // Act
        await provider.InitializeAsync(_client);

        // Assert
        Func<IReadOnlyList<string>> supportedLocales = () => provider.SupportedLocales;
        Func<string> defaultLocale = () => provider.DefaultLocale;
        supportedLocales.Should().Throw<InvalidOperationException>();
        defaultLocale.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task InitializeAsync_BecomesUnresolved_WhenSquidexReturnsEmpty()
    {
        // Arrange
        var options = SquidexFakes.AppOptionsWithoutLocales();
        var provider = new SquidexLocaleProvider(options, NullLogger<SquidexLocaleProvider>.Instance);

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(new List<SquidexLocaleInfo>() as IReadOnlyList<SquidexLocaleInfo>);

        // Act
        await provider.InitializeAsync(_client);

        // Assert
        Func<IReadOnlyList<string>> act = () => provider.SupportedLocales;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task InitializeAsync_RefetchesOnEveryCall_ForOnDemandResync()
    {
        // Arrange — not fully explicit, so InitializeAsync can be called again after startup
        // (e.g. to pick up a locale added in Squidex without restarting the app).
        var options = SquidexFakes.AppOptionsWithoutLocales();
        var provider = new SquidexLocaleProvider(options, NullLogger<SquidexLocaleProvider>.Instance);

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeLocales(TestLocales.UkUA, TestLocales.En));

        // Act
        await provider.InitializeAsync(_client);
        await provider.InitializeAsync(_client); // second call — re-syncs, not a no-op

        // Assert — fetched again on the second call
        await _client.Received(2).GetAppLocalesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeAsync_NeverCallsSquidex_WhenBothExplicitlyConfigured_EvenOnRepeatedCalls()
    {
        // Arrange — both DefaultLocale and SupportedLocales explicit
        var provider = new SquidexLocaleProvider(SquidexFakes.DefaultAppOptions(), NullLogger<SquidexLocaleProvider>.Instance);

        // Act
        await provider.InitializeAsync(_client);
        await provider.InitializeAsync(_client);

        // Assert — appsettings always wins, network never needed
        await _client.DidNotReceive().GetAppLocalesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeAsync_RetriesOnNextCall_AfterAPriorFetchFailed()
    {
        // Arrange — first attempt fails (transient outage), second attempt succeeds
        var options = SquidexFakes.AppOptionsWithoutLocales();
        var provider = new SquidexLocaleProvider(options, NullLogger<SquidexLocaleProvider>.Instance);

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Throws(new HttpRequestException("Connection refused"));

        // Act — first call fails, falls back
        await provider.InitializeAsync(_client);

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeLocales(TestLocales.UkUA, TestLocales.En, TestLocales.De));

        // second call — retries, this time succeeds
        await provider.InitializeAsync(_client);

        // Assert — a failed attempt is not cached as final; the fuller list from the retry wins
        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA, TestLocales.En, TestLocales.De]);
        await _client.Received(2).GetAppLocalesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeAsync_DiscardsStaleFetchedList_WhenALaterRefreshFails()
    {
        // Arrange — first refresh succeeds with a fuller list, second refresh fails
        var options = SquidexFakes.AppOptionsWithoutLocales();
        var provider = new SquidexLocaleProvider(options, NullLogger<SquidexLocaleProvider>.Instance);

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeLocales(TestLocales.UkUA, TestLocales.En, TestLocales.De));

        await provider.InitializeAsync(_client);
        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA, TestLocales.En, TestLocales.De]);

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Throws(new HttpRequestException("Connection refused"));

        // Act — a later refresh fails
        await provider.InitializeAsync(_client);

        // Assert — the previously fetched (now stale) fuller list is discarded, not silently kept
        Func<IReadOnlyList<string>> act = () => provider.SupportedLocales;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task InitializeAsync_ForceRefresh_BypassesBothExplicitSkip()
    {
        // Arrange — both explicit, would normally never call Squidex
        var provider = new SquidexLocaleProvider(SquidexFakes.DefaultAppOptions(), NullLogger<SquidexLocaleProvider>.Instance);

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeLocales(masterLocale: TestLocales.UkUA, TestLocales.En));

        // Act
        await provider.InitializeAsync(_client, forceRefresh: true);

        // Assert — forced refresh actually hit Squidex
        await _client.Received(1).GetAppLocalesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeAsync_ForceRefresh_BecomesUnresolved_WhenFetchFails()
    {
        // Arrange — both explicit; forced refresh fails. Even explicit config doesn't
        // survive a failed check against Squidex, the sole guarantor of locales.
        var provider = new SquidexLocaleProvider(SquidexFakes.DefaultAppOptions(), NullLogger<SquidexLocaleProvider>.Instance);

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Throws(new HttpRequestException("Connection refused"));

        // Act
        await provider.InitializeAsync(_client, forceRefresh: true);

        // Assert
        Func<string> defaultLocale = () => provider.DefaultLocale;
        Func<IReadOnlyList<string>> supportedLocales = () => provider.SupportedLocales;
        defaultLocale.Should().Throw<InvalidOperationException>();
        supportedLocales.Should().Throw<InvalidOperationException>();
    }
}