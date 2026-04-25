using System.Net;

namespace Coworking.External.Squidex.Exceptions;

public sealed class SquidexApiException(
    HttpStatusCode statusCode,
    string message,
    string[]? details = null)
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string[] Details { get; } = details ?? [];
}