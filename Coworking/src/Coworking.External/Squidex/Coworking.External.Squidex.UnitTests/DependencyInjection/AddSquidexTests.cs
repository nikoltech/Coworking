using Coworking.External.Squidex.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Coworking.External.Squidex.UnitTests.DependencyInjection;

public sealed class AddSquidexTests
{
    private static IConfiguration Config(params (string Key, string Value)[] extra)
    {
        var settings = new Dictionary<string, string?>
        {
            [$"Squidex:Apps:{TestApps.Default}:BaseUrl"] = TestUrls.BaseUrl,
            [$"Squidex:Apps:{TestApps.Default}:AppName"] = TestApps.Default,
            [$"Squidex:Apps:{TestApps.Default}:DefaultClient"] = TestClientNames.Default,
            [$"Squidex:Apps:{TestApps.Default}:Clients:{TestClientNames.Default}:ClientId"] = "app:default",
            [$"Squidex:Apps:{TestApps.Default}:Clients:{TestClientNames.Default}:ClientSecret"] = "secret",
        };

        foreach (var (key, value) in extra)
            settings[key] = value;

        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    [Fact]
    public void AddSquidex_Succeeds_WithoutLocaleKeys()
    {
        var act = () => new ServiceCollection().AddSquidex(Config());

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("DefaultLocale", "uk-UA")]
    [InlineData("SupportedLocales:0", "uk-UA")]
    public void AddSquidex_Throws_OnLegacyLocaleKey(string key, string value)
    {
        var config = Config(($"Squidex:Apps:{TestApps.Default}:{key}", value));

        var act = () => new ServiceCollection().AddSquidex(config);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*locales are no longer configured*");
    }
}
