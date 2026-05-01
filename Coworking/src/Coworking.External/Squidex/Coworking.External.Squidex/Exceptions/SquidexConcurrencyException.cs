namespace Coworking.External.Squidex.Exceptions;

public sealed class SquidexConcurrencyException(string message)
    : Exception(message);