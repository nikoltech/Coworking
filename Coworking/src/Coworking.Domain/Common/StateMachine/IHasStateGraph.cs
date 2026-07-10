namespace StateMachine;

/// Marks an entity that owns its state graph. Each entity declares its own.
/// Requires C# 11 / .NET 7+ for static abstract members. For older targets,
/// replace with an instance property.
public interface IHasStateGraph<T> where T : notnull
{
    static abstract StateGraph<T> StateGraph { get; }
}
