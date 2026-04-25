namespace Coworking.External.Squidex.Abstractions.Filters;

public sealed class FilterLogical : Dictionary<string, object[]>
{
    public FilterLogical(string op, params object[] filters)
    {
        Add(op, filters);
    }
}