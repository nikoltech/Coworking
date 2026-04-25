using RichardSzalay.MockHttp;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Coworking.External.Squidex.UnitTests.Helpers;

internal static class MockHttpExtensions
{
    public static HttpClient ToHttpClient(this MockHttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("https://cloud.squidex.io") };

    public static MockedRequest RespondJson<T>(
        this MockedRequest request, T body) =>
        request.Respond(
            HttpStatusCode.OK,
            new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"));

    public static MockedRequest RespondError(
        this MockedRequest request,
        HttpStatusCode statusCode,
        string message) =>
        request.Respond(
            statusCode,
            new StringContent(
                JsonSerializer.Serialize(new { message }),
                Encoding.UTF8,
                "application/json"));
}