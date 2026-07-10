namespace StateMachine;

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
        private readonly HashSet<(T From, T To)> _blocked = new();

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

        /// Remove one FromAnywhere edge. Explicit From edges are never affected.
        public Builder Block(T from, T to)
        {
            ArgumentNullException.ThrowIfNull(from);
            ArgumentNullException.ThrowIfNull(to);
            _blocked.Add((from, to));
            return this;
        }

        public StateGraph<T> Build()
        {
            foreach (var (from, to) in _blocked)
                if (!_globalTargets.Contains(to))
                    throw new InvalidOperationException(
                        $"Block({from}, {to}): {to} is not a FromAnywhere target");

            // includes states that appear only as targets, so they receive globals too
            var allStates = _edges.Keys
                .Concat(_edges.Values.SelectMany(set => set))
                .Concat(_globalTargets)
                .ToHashSet();

            foreach (var state in allStates)
                foreach (var target in _globalTargets)
                    if (!EqualityComparer<T>.Default.Equals(state, target) &&
                        !_blocked.Contains((state, target)))
                        Edge(state).Add(target);

            return new StateGraph<T>(
                _edges.ToDictionary(k => k.Key, k => (IReadOnlySet<T>)k.Value));
        }
    }
}