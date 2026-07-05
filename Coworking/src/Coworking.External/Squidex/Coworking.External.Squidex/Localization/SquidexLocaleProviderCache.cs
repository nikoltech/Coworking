using Coworking.External.Squidex.Abstractions.Options;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Coworking.External.Squidex.Localization;

/// <summary>
/// Singleton cache for locale providers — one per app.
/// Avoids recreating providers on every request.
/// </summary>
public sealed class SquidexLocaleProviderCache(ILoggerFactory loggerFactory)
{
    private readonly ConcurrentDictionary<string, SquidexLocaleProvider> _providers = new();

    public SquidexLocaleProvider GetOrCreate(string appName, SquidexAppOptions appOptions) =>
        _providers.GetOrAdd(appName, _ =>
            new SquidexLocaleProvider(appOptions, loggerFactory.CreateLogger<SquidexLocaleProvider>()));
}