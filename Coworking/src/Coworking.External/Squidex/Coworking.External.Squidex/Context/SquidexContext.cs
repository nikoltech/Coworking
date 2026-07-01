using Coworking.External.Squidex.Abstractions.Client;
using Coworking.External.Squidex.Abstractions.Context;
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Pagination;
using Coworking.External.Squidex.Abstractions.Set;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Set;

namespace Coworking.External.Squidex.Context;

/// <summary>
/// Squidex app context. Analogous to EF DbContext.
/// Ready to use straight from DI (keyed by app name), or subclass to expose typed
/// set properties (e.g. <c>Cities</c>).
///
/// Usage:
///   var cities = await context.Set&lt;CitySchema&gt;().QueryAsync(...);
///   var cities = await context.UsingClient("Frontend").Set&lt;CitySchema&gt;().QueryAsync(...);
/// </summary>
public class SquidexContext : ISquidexContext
{
    private readonly ISquidexPaginator _paginator;
    private readonly SquidexClientFactory _clientFactory;
    private readonly string _appName;
    private readonly ISquidexSets _default;

    public SquidexContext(ISquidexApiClient defaultClient, ISquidexPaginator paginator,
        SquidexClientFactory clientFactory,
        string appName)
    {
        _paginator = paginator;
        _clientFactory = clientFactory;
        _appName = appName;
        _default = new SquidexSets(defaultClient, paginator);
    }

    /// <inheritdoc/>
    public ISquidexSet<T> Set<T>(string schema) where T : class =>
        _default.Set<T>(schema);

    /// <inheritdoc/>
    public ISquidexSet<T> Set<T>() where T : class, ISquidexSchema =>
        _default.Set<T>();

    /// <inheritdoc/>
    public ISquidexSets UsingClient(string clientName) =>
        new SquidexSets(_clientFactory.CreateForApp(_appName, clientName), _paginator);
}

/// <summary>
/// Set accessor bound to a single client (credentials). Shared by the context's
/// default client and by <see cref="ISquidexContext.UsingClient"/>.
/// </summary>
internal sealed class SquidexSets(ISquidexApiClient client, ISquidexPaginator paginator)
    : ISquidexSets
{
    public ISquidexSet<T> Set<T>(string schema) where T : class =>
        new SquidexSet<T>(client, paginator, schema);

    public ISquidexSet<T> Set<T>() where T : class, ISquidexSchema =>
        Set<T>(T.SchemaName);
}
