// Helpers/SquidexFakes.cs
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.UnitTests.Helpers;

internal static class SquidexFakes
{
    // ── Options ──────────────────────────────────────────────────────────────

    public static SquidexOptions DefaultOptions(
        string baseUrl = "https://fake.cloud.squidex.io",
        string appName = "test-app") =>
        new()
        {
            BaseUrl = baseUrl,
            AppName = appName,
            MaxPageSize = 3,
            DefaultLocale = TestLocales.UkUA,
            SupportedLocales = [TestLocales.UkUA, TestLocales.En],
            Clients = new Dictionary<string, SquidexClientCredentials>
            {
                [TestClientNames.Default] = new() { ClientId = "app:default", ClientSecret = "secret-1" },
                [TestClientNames.Frontend] = new() { ClientId = "app:frontend", ClientSecret = "secret-2" }
            }
        };

    public static IOptions<SquidexOptions> OptionsMock(SquidexOptions? options = null) =>
        Microsoft.Extensions.Options.Options.Create(options ?? DefaultOptions());

    // ── Content factories ─────────────────────────────────────────────────────

    public static ContentDto<T> MakeContent<T>(
        T data,
        string id = "test-id",
        string status = TestStatuses.Published) =>
        new(id, 1, DateTime.UtcNow, DateTime.UtcNow, status, data);

    public static ContentDto<T> MakeDraft<T>(T data, string id = "draft-id") =>
        MakeContent(data, id, TestStatuses.Draft);

    public static ContentDto<T> MakeArchived<T>(T data, string id = "archived-id") =>
        MakeContent(data, id, TestStatuses.Archived);

    public static ResponseSchema<T> MakeResponse<T>(
        params T[] items) =>
        new(items.Length,
            items.Select((item, i) => MakeContent(item, $"id-{i + 1}")).ToList());

    public static ResponseSchema<T> MakePagedResponse<T>(
        long total,
        string status = TestStatuses.Published,
        params T[] items) =>
        new(total,
            items.Select((item, i) => MakeContent(item, $"id-{i + 1}", status)).ToList());

    // ── JSON helpers ──────────────────────────────────────────────────────────

    public static string TokenJson(
        string token = "test-access-token",
        int expiresIn = 3600) =>
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

    public static string ResponseJson<T>(ResponseSchema<T> response) =>
        JsonSerializer.Serialize(response);

    public static string ContentJson<T>(ContentDto<T> content) =>
        JsonSerializer.Serialize(content);

    // ── Test schemas ──────────────────────────────────────────────────────────

    public sealed class TestSchema
    {
        [JsonPropertyName("Name")]
        public IvField<string>? Name { get; set; }

        [JsonPropertyName("Title")]
        public LocalizedField<string>? Title { get; set; }

        [JsonPropertyName("IsActive")]
        public IvField<bool?>? IsActive { get; set; }
    }

    public static TestSchema MakeTestSchema(
        string? name = null,
        bool? active = null) =>
        new()
        {
            Name = name is not null ? new IvField<string>(name) : null,
            IsActive = active is not null ? new IvField<bool?>(active) : null
        };
}