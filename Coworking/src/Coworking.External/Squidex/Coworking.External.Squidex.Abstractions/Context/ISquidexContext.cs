using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Set;

namespace Coworking.External.Squidex.Abstractions.Context;

/// <summary>
/// Access to Squidex schemas over one client (credentials).
/// </summary>
public interface ISquidexSets
{
    /// <summary>
    /// Returns a queryable/writable set for the given schema.
    /// Analogous to EF DbContext.Set&lt;T&gt;().
    /// </summary>
    ISquidexSet<T> Set<T>(string schema) where T : class;

    /// <summary>
    /// Returns a queryable/writable set for a schema DTO that declares its own name.
    /// Shorthand for <see cref="Set{T}(string)"/> with <c>T.SchemaName</c>.
    /// </summary>
    ISquidexSet<T> Set<T>() where T : class, ISquidexSchema;
}

/// <summary>
/// Entry point for Squidex schema access. Analogous to EF DbContext.
/// Ready to use from DI (keyed by app name), or subclass to expose typed set properties.
/// </summary>
public interface ISquidexContext : ISquidexSets
{
    /// <summary>
    /// Returns a set accessor using the specified client credentials.
    /// Use when a schema requires non-default credentials.
    /// </summary>
    ISquidexSets UsingClient(string clientName);
}
