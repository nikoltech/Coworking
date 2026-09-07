using Coworking.External.Squidex.Abstractions.Client;
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Options;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Localization;
using Coworking.External.Squidex.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RichardSzalay.MockHttp;

namespace Coworking.External.Squidex.UnitTests.Localization;

public sealed class SquidexLocaleProviderTests
{
    private readonly ISquidexApiClient _client = Substitute.For<ISquidexApiClient>();

    private static SquidexLocaleProvider Provider(Func<SquidexAppOptions, SquidexAppOptions> configure) =>
        new(configure(SquidexFakes.DefaultAppOptions()), NullLogger<SquidexLocaleProvider>.Instance);

    // ── Resolved from configuration alone ────────────────────────────────────

    [Fact]
    public void BothConfigured_ResolvesWithoutSquidex()
    {
        var provider = Provider(o => o);

        provider.DefaultLocale.Should().Be(TestLocales.UkUA);
        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA, TestLocales.En]);
    }

    [Fact]
    public void DefaultLocaleAlone_ResolvesToSingleLocale()
    {
        var provider = Provider(o => o with { SupportedLocales = [] });

        provider.DefaultLocale.Should().Be(TestLocales.UkUA);
        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA]);
    }

    [Fact]
    public void SupportedLocalesAlone_StaysUnresolved()
    {
        var provider = Provider(o => o with { DefaultLocale = string.Empty });

        Func<string> defaultLocale = () => provider.DefaultLocale;
        Func<IReadOnlyList<string>> supported = () => provider.SupportedLocales;

        defaultLocale.Should().Throw<InvalidOperationException>();
        supported.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void NothingConfigured_StaysUnresolved()
    {
        var provider = Provider(o => o with { DefaultLocale = string.Empty, SupportedLocales = [] });

        Func<string> act = () => provider.DefaultLocale;

        act.Should().Throw<InvalidOperationException>();
    }

    // ── SyncAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SyncAsync_FillsBoth_WhenNothingConfigured()
    {
        var provider = Provider(o => o with { DefaultLocale = string.Empty, SupportedLocales = [] });

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeLocales(TestLocales.UkUA, TestLocales.En, TestLocales.De));

        await provider.SyncAsync(_client);

        provider.DefaultLocale.Should().Be(TestLocales.UkUA);
        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA, TestLocales.En, TestLocales.De]);
    }

    [Fact]
    public async Task SyncAsync_FillsDefaultOnly_WhenSupportedLocalesConfigured()
    {
        var provider = Provider(o => o with { DefaultLocale = string.Empty });

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeLocales(TestLocales.UkUA, TestLocales.En, TestLocales.De));

        await provider.SyncAsync(_client);

        provider.DefaultLocale.Should().Be(TestLocales.UkUA);
        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA, TestLocales.En]);
    }

    [Fact]
    public async Task SyncAsync_KeepsConfiguredValues()
    {
        var provider = Provider(o => o);

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeLocales(TestLocales.UkUA, TestLocales.En, TestLocales.De));

        await provider.SyncAsync(_client);

        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA, TestLocales.En]);
    }

    [Fact]
    public async Task SyncAsync_Throws_WhenConfiguredDefaultIsNotMaster()
    {
        var provider = Provider(o => o with { DefaultLocale = TestLocales.De, SupportedLocales = [] });

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeLocales(TestLocales.UkUA, TestLocales.En));

        var act = () => provider.SyncAsync(_client);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*de*uk-UA*");
    }

    [Fact]
    public async Task SyncAsync_Throws_WhenSquidexReturnsNoLanguages()
    {
        var provider = Provider(o => o with { DefaultLocale = string.Empty, SupportedLocales = [] });

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(new List<SquidexLocaleInfo>() as IReadOnlyList<SquidexLocaleInfo>);

        var act = () => provider.SyncAsync(_client);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SyncAsync_PropagatesTransportFailure()
    {
        var provider = Provider(o => o with { DefaultLocale = string.Empty, SupportedLocales = [] });

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Throws(new HttpRequestException("Connection refused"));

        var act = () => provider.SyncAsync(_client);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task SyncAsync_LeavesPreviousStateIntact_WhenItFails()
    {
        var provider = Provider(o => o with { SupportedLocales = [] });

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Throws(new HttpRequestException("Connection refused"));

        var act = () => provider.SyncAsync(_client);
        await act.Should().ThrowAsync<HttpRequestException>();

        provider.DefaultLocale.Should().Be(TestLocales.UkUA);
        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA]);
    }

    [Fact]
    public async Task SyncAsync_RefetchesOnEveryCall()
    {
        var provider = Provider(o => o with { DefaultLocale = string.Empty, SupportedLocales = [] });

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeLocales(TestLocales.UkUA, TestLocales.En));

        await provider.SyncAsync(_client);
        await provider.SyncAsync(_client);

        await _client.Received(2).GetAppLocalesAsync(Arg.Any<CancellationToken>());
    }

    // ── ValidateAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_Passes_WhenConfiguredDefaultIsMaster()
    {
        var provider = Provider(o => o);

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeLocales(TestLocales.UkUA, TestLocales.En));

        await provider.ValidateAsync(_client);

        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA, TestLocales.En]);
    }

    [Fact]
    public async Task ValidateAsync_Throws_WhenConfiguredDefaultIsNotMaster()
    {
        var provider = Provider(o => o with { DefaultLocale = TestLocales.De });

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeLocales(TestLocales.UkUA, TestLocales.En));

        var act = () => provider.ValidateAsync(_client);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*de*uk-UA*");
    }

    [Fact]
    public async Task ValidateAsync_DoesNotChangeState()
    {
        var provider = Provider(o => o with { SupportedLocales = [] });

        _client.GetAppLocalesAsync(Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeLocales(TestLocales.UkUA, TestLocales.En, TestLocales.De));

        await provider.ValidateAsync(_client);

        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA]);
    }

    // ── Resolving locales must not itself need locales ───────────────────────

    [Fact]
    public async Task SyncAsync_ReachesSquidex_WhenNoLocalesAreKnown()
    {
        var options = SquidexFakes.DefaultAppOptions() with
        {
            DefaultLocale = string.Empty,
            SupportedLocales = []
        };

        var provider = new SquidexLocaleProvider(options, NullLogger<SquidexLocaleProvider>.Instance);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*/api/apps/*/languages")
            .Respond("application/json", SquidexFakes.AppLanguagesJson(TestLocales.UkUA, TestLocales.En));

        var client = new SquidexApiClient(
            mockHttp.ToHttpClient(), options, TestClientNames.Default, provider);

        await provider.SyncAsync(client);

        provider.DefaultLocale.Should().Be(TestLocales.UkUA);
        provider.SupportedLocales.Should().BeEquivalentTo([TestLocales.UkUA, TestLocales.En]);
    }
}
