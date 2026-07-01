namespace Coworking.External.Squidex.Client;

internal static class HttpRequestMessageExtensions
{
    /// <summary>
    /// Deep-copies a request so it can be re-sent — a sent <see cref="HttpRequestMessage"/>
    /// cannot be reused. Shared by retry loops (transient errors) and the auth handler (401 refresh).
    /// </summary>
    public static async Task<HttpRequestMessage> CloneAsync(
        this HttpRequestMessage source, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(source.Method, source.RequestUri);

        foreach (var header in source.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        foreach (var option in source.Options)
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);

        if (source.Content is null)
            return clone;

        var bytes = await source.Content.ReadAsByteArrayAsync(ct);
        clone.Content = new ByteArrayContent(bytes);

        foreach (var header in source.Content.Headers)
            clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }
}
