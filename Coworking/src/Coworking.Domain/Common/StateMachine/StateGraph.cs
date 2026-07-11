namespace Coworking.Domain.Common.StateMachine;

/// Directed graph of allowed transitions between states of type T
/// (enum, string, or any value with correct equality/hash).
public sealed class StateGraph<T> where T : notnull
{
    private readonly IReadOnlyDictionary<T, IReadOnlySet<T>> _edges;
    private StateGraph(IReadOnlyDictionary<T, IReadOnlySet<T>> edges) => _edges = edges;

    public bool CanMove(T from, T to) =>
        _edges.TryGetValue(from, out var next) && next.Contains(to);

    public IReadOnlySet<T> From(T state) =>
        _edges.TryGetValue(state, out var next) ? next : Empty;

    private static readonly IReadOnlySet<T> Empty = new HashSet<T>();

    public static Builder Create() => new();

    public sealed class Builder
    {
        private readonly Dictionary<T, HashSet<T>> _edges = new();
        private readonly HashSet<T> _globalTargets = new();
        private readonly HashSet<(T From, T To)> _excluded = new();

        private HashSet<T> Edge(T s) => _edges.TryGetValue(s, out var x) ? x : _edges[s] = new();

        /// Explicit transitions from a state. A state never used as a source is terminal.
        public Builder From(T state, params T[] targets)
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(targets);
            foreach (var t in targets)
                ArgumentNullException.ThrowIfNull(t);

            Edge(state).UnionWith(targets);
            return this;
        }

        /// Targets reachable from every known state.
        public Builder FromAnywhere(params T[] targets)
        {
            if (targets is null) return this;
            foreach (var t in targets)
            {
                ArgumentNullException.ThrowIfNull(t);
                _globalTargets.Add(t);
            }
            return this;
        }

        /// Exclude a transition (from -> to), whether added by From or FromAnywhere.
        /// Applied last; throws at Build if the edge does not exist.
        public Builder Exclude(T from, T to)
        {
            ArgumentNullException.ThrowIfNull(from);
            ArgumentNullException.ThrowIfNull(to);
            _excluded.Add((from, to));
            return this;
        }

        public StateGraph<T> Build()
        {
            // includes states that appear only as targets, so they receive globals too
            var allStates = _edges.Keys
                .Concat(_edges.Values.SelectMany(set => set))
                .Concat(_globalTargets)
                .ToHashSet();

            foreach (var state in allStates)
                foreach (var target in _globalTargets)
                    if (!EqualityComparer<T>.Default.Equals(state, target))
                        Edge(state).Add(target);

            // exclusions applied last, over the fully built graph
            foreach (var (from, to) in _excluded)
            {
                var removed = _edges.TryGetValue(from, out var set) && set.Remove(to);
                if (!removed)
                    throw new InvalidOperationException(
                        $"Exclude({from}, {to}): no such transition to remove");
            }

            return new StateGraph<T>(
                _edges.ToDictionary(k => k.Key, k => (IReadOnlySet<T>)k.Value));
        }
    }
}