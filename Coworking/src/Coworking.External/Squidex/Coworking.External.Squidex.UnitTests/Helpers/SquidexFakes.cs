using Coworking.External.Squidex.Abstractions.Localization;
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.UnitTests.Helpers;

internal static class SquidexFakes
{
    public static SquidexOptions DefaultOptions(
        string baseUrl = "https://cloud.squidex.io",
        string appName = "test-app") =>
        new()
        {
            BaseUrl = baseUrl,
            AppName = appName,
            MaxPageSize = 3,
            DefaultLocale = SquidexLocales.UkUA,
            SupportedLocales = [SquidexLocales.UkUA, SquidexLocales.En],
            Clients = new Dictionary<string, SquidexClientCredentials>
            {
                ["Default"] = new() { ClientId = "app:default", ClientSecret = "secret" },
                ["Frontend"] = new() { ClientId = "app:frontend", ClientSecret = "secret2" }
            }
        };

    public static IOptions<SquidexOptions> DefaultOptionsMock(
        SquidexOptions? options = null) =>
        Microsoft.Extensions.Options.Options.Create(options ?? DefaultOptions());

    public static ContentDto<T> MakeContent<T>(T data, string id = "abc123") =>
        new(id, 1, DateTime.UtcNow, DateTime.UtcNow, "Published", data);

    public static ResponseSchema<T> MakeResponse<T>(
        params T[] items) =>
        new(items.Length,
            items.Select(i => MakeContent(i)).ToList());

    public static ResponseSchema<T> MakePagedResponse<T>(
        long total, params T[] items) =>
        new(total,
            items.Select(i => MakeContent(i)).ToList());

    public static string TokenJson(
        string token = "test-token", int expiresIn = 3600) =>
        JsonSerializer.Serialize(new
        {
            access_token = token,
            token_type = "Bearer",
            expires_in = expiresIn
        });

    public static string AppLanguagesJson(params string[] locales) =>
        JsonSerializer.Serialize(new
        {
            items = locales.Select(l => new { iso2Code = l })
        });

    public sealed record TestSchema(
        [property: JsonPropertyName("Name")] IvField<string>? Name = null,
        [property: JsonPropertyName("Title")] LocalizedField<string>? Title = null);
}