using Coworking.External.Squidex.Abstractions.Repository;
using Coworking.External.Squidex.Auth;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Localization;
using Coworking.External.Squidex.Options;
using Microsoft.Extensions.Options;

public sealed class SquidexClientFactory(
    IHttpClientFactory httpClientFactory,
    IOptions<SquidexOptions> options,
    SquidexLocaleProvider locales)
{
    private readonly SquidexOptions _options = options.Value;

    public ISquidexApiClient Create(string clientName = SquidexAuthHandler.DefaultClient)
    {
        EnsureClientExists(clientName);
        var http = httpClientFactory.CreateClient(SquidexHttpClientNames.Api);
        return new SquidexApiClient(http, _options, clientName, locales);
    }

    public SquidexAssetClient CreateAssetClient(
        string clientName = SquidexAuthHandler.DefaultClient)
    {
        EnsureClientExists(clientName);
        var http = httpClientFactory.CreateClient(SquidexHttpClientNames.Api);
        return new SquidexAssetClient(http, _options, clientName);
    }

    private void EnsureClientExists(string clientName)
    {
        if (!_options.Clients.ContainsKey(clientName))
            throw new InvalidOperationException(
                $"Squidex client '{clientName}' is not configured. " +
                $"Available: {string.Join(", ", _options.Clients.Keys)}");
    }
}