namespace StateMachine;

/// A single recorded transition, for history.
public readonly record struct StateChange<T>(T From, T To, DateTime At) where T : notnull;
