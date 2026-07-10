namespace StateMachine;

/// Thrown when a transition is not allowed by the graph.
public sealed class InvalidTransitionException<T> : Exception where T : notnull
{
    public T From { get; }
    public T To { get; }

    public InvalidTransitionException(T from, T to)
        : base($"Transition {from} -> {to} is not allowed")
        => (From, To) = (from, to);
}
