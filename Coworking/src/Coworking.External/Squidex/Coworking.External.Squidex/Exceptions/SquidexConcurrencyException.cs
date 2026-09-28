using System.Net;

namespace Coworking.External.Squidex.Exceptions;

public sealed class SquidexConcurrencyException(string message)
    : SquidexApiException(HttpStatusCode.PreconditionFailed, message);
