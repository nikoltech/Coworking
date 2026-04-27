using Coworking.External.Squidex.Abstractions.Repository;

namespace Coworking.External.Squidex.Abstractions.Context;

/// <summary>
/// Entry point for Squidex schema access. Analogous to EF DbContext.
/// One context per Squidex app.
/// </summary>
public interface ISquidexContext
{
    /// <summary>
    /// Returns a queryable/writable set for the given schema.
    /// Analogous to EF DbContext.Set&lt;T&gt;().
    /// </summary>
    ISquidexRepository<T> Set<T>(string schema) where T : class;

    /// <summary>
    /// Returns a context accessor using the specified client credentials.
    /// Use when a schema requires non-default credentials.
    /// </summary>
    ISquidexClientScope UsingClient(string clientName);
}

/// <summary>
/// Scoped accessor for a specific client within an app context.
/// </summary>
public interface ISquidexClientScope
{
    ISquidexRepository<T> Set<T>(string schema) where T : class;
}