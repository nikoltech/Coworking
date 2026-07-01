// Helpers/MockHttpExtensions.cs
using RichardSzalay.MockHttp;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Coworking.External.Squidex.UnitTests.Helpers;

internal static class MockHttpExtensions
{
    public static HttpClient ToHttpClient(
        this MockHttpMessageHandler handler,
        string baseUrl = TestUrls.BaseUrl) =>
        new(handler) { BaseAddress = new Uri(baseUrl) };

    // Content is built per-invocation (not a single shared instance) so the mock
    // stays valid across retries — HttpResponseMessage.Dispose() disposes its Content too.

    public static MockedRequest RespondJson<T>(
        this MockedRequest request, T body) =>
        request.Respond(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json")
        });

    public static MockedRequest RespondError(
        this MockedRequest request,
        HttpStatusCode statusCode,
        string message,
        string[]? details = null) =>
        request.Respond(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { message, details }),
                Encoding.UTF8,
                "application/json")
        });

    public static MockedRequest RespondEmptySchema<T>(this MockedRequest request) =>
        request.RespondJson(SquidexFakes.MakeResponse<T>());
}