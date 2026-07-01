using Coworking.External.Squidex.Abstractions.Client;
using Coworking.External.Squidex.Abstractions.Context;
using Coworking.External.Squidex.Abstractions.Pagination;
using Coworking.External.Squidex.Abstractions.Set;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Set;

namespace Coworking.External.Squidex.Context;

/// <summary>
/// Base Squidex context.
/// One subclass per Squidex app.
///
/// Usage:
///   var cities = await context.Set&lt;CitySchema&gt;("city").QueryAsync(...);
///   var cities = await context.UsingClient("Frontend").Set&lt;CitySchema&gt;("city").QueryAsync(...);
/// </summary>
public abstract class SquidexContext : ISquidexContext
{
    private readonly ISquidexApiClient _defaultClient;
    private readonly ISquidexPaginator _paginator;
    private readonly SquidexClientFactory _clientFactory;
    private readonly string _appName;

    protected SquidexContext(ISquidexApiClient defaultClient, ISquidexPaginator paginator,
        SquidexClientFactory clientFactory,
        string appName)
    {
        _defaultClient = defaultClient;
        _paginator = paginator;
        _clientFactory = clientFactory;
        _appName = appName;
    }

    /// <inheritdoc/>
    public ISquidexSet<T> Set<T>(string schema) where T : class =>
        new SquidexSet<T>(_defaultClient, _paginator, schema);

    /// <inheritdoc/>
    public ISquidexClientScope UsingClient(string clientName) =>
        new SquidexClientScope(
            _clientFactory.CreateForApp(_appName, clientName),
            _paginator);

    // ── Protected helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Creates a typed repository for a schema. Use for exposing typed properties on the context.
    /// Analogous to EF DbSet&lt;T&gt; properties.
    /// </summary>
    protected TSet CreateSet<TSet, TData>(string schema)
        where TSet : SquidexSet<TData>
        where TData : class =>
        (TSet)Activator.CreateInstance(typeof(TSet), _defaultClient, _paginator, schema)!;
}

/// <summary>
/// Scoped accessor for a specific named client within an app context.
/// </summary>
internal sealed class SquidexClientScope(
    ISquidexApiClient client,
    ISquidexPaginator paginator)
    : ISquidexClientScope
{
    public ISquidexSet<T> Set<T>(string schema) where T : class =>
        new SquidexSet<T>(client, paginator, schema);
}