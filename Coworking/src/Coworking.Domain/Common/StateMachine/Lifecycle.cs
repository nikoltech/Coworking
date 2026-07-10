namespace StateMachine;

/// Holds the current state and transition history, guarded by a graph.
/// Attach one to any entity that has a lifecycle.
public sealed class Lifecycle<T> where T : notnull
{
    private readonly StateGraph<T> _graph;
    private readonly List<StateChange<T>> _history = new();

    public T Current { get; private set; }
    public IReadOnlyList<StateChange<T>> History => _history;

    public Lifecycle(T initial, StateGraph<T> graph)
        => (Current, _graph) = (initial, graph);

    public bool CanMove(T to) => _graph.CanMove(Current, to);

    /// Reachable states from the current one.
    public IReadOnlySet<T> Available => _graph.From(Current);

    /// Move to a new state; throws if the graph forbids it.
    public void MoveTo(T to)
    {
        if (!_graph.CanMove(Current, to))
            throw new InvalidTransitionException<T>(Current, to);

        _history.Add(new StateChange<T>(Current, to, DateTime.UtcNow));
        Current = to;
    }

    /// Restore from storage without graph validation (data is already valid).
    public void Rehydrate(T state, IEnumerable<StateChange<T>> history)
    {
        Current = state;
        _history.Clear();
        _history.AddRange(history);
    }
}
