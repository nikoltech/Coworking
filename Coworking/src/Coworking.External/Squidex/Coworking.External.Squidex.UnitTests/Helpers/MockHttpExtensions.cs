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
        string baseUrl = "https://cloud.squidex.io") =>
        new(handler) { BaseAddress = new Uri(baseUrl) };

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
        string message,
        string[]? details = null) =>
        request.Respond(
            statusCode,
            new StringContent(
                JsonSerializer.Serialize(new { message, details }),
                Encoding.UTF8,
                "application/json"));

    public static MockedRequest RespondEmptySchema<T>(this MockedRequest request) =>
        request.RespondJson(SquidexFakes.MakeResponse<T>());
}