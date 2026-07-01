namespace Coworking.External.Squidex.Abstractions.Models;

/// <summary>
/// Marks a content DTO with its Squidex schema name so it can be resolved from the
/// type alone — enables <c>context.Set&lt;T&gt;()</c> without passing the schema string.
/// </summary>
public interface ISquidexSchema
{
    static abstract string SchemaName { get; }
}
