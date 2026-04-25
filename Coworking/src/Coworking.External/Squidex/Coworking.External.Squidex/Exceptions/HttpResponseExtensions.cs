using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Exceptions;

public static class HttpResponseExtensions
{
    private static readonly JsonSerializerOptions Json =
        new() { PropertyNameCaseInsensitive = true };

    public static async Task EnsureSquidexSuccessAsync(this HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        SquidexErrorBody? error = null;
        try
        {
            error = await response.Content
                .ReadFromJsonAsync<SquidexErrorBody>(Json, ct);
        }
        catch
        {
            // Response is not JSON — fall through to status-based message
        }

        var message = error?.Message ?? response.ReasonPhrase ?? "Unknown Squidex error.";
        throw new SquidexApiException(response.StatusCode, message, error?.Details);
    }

    private sealed record SquidexErrorBody(
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("details")] string[]? Details);
}