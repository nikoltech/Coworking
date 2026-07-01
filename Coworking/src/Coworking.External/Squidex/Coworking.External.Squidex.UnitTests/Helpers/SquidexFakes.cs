// Helpers/SquidexFakes.cs
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.UnitTests.Helpers;

internal static class SquidexFakes
{
	// ── Options ──────────────────────────────────────────────────────────────

	public static SquidexAppOptions DefaultAppOptions(
		string baseUrl = TestUrls.BaseUrl,
		string appName = TestApps.Default) =>
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

	public static SquidexAppOptions AppOptionsWithoutLocales(
		string baseUrl = TestUrls.BaseUrl,
		string appName = TestApps.Default) =>
		DefaultAppOptions(baseUrl, appName) with { SupportedLocales = [] };

	public static IOptions<SquidexGlobalOptions> GlobalOptionsMock(SquidexAppOptions? appOptions = null)
	{
		var app = appOptions ?? DefaultAppOptions();
		return Microsoft.Extensions.Options.Options.Create(new SquidexGlobalOptions
		{
			Apps = new Dictionary<string, SquidexAppOptions> { [app.AppName] = app }
		});
	}

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

	public static ResponseSchema<T> MakeResponse<T>(params T[] items) =>
		new(items.Length,
			items.Select((item, i) => MakeContent(item, $"id-{i + 1}")).ToList());

	public static ResponseSchema<T> MakePagedResponse<T>(
		long total,
		string status = TestStatuses.Published,
		params T[] items) =>
		new(total,
			items.Select((item, i) => MakeContent(item, $"id-{i + 1}", status)).ToList());

	// ── Asset factories ─────────────────────────────────────────────────────────

	public static AssetDto MakeAsset(
		string id = "asset-1",
		string fileName = "photo.png",
		string mimeType = "image/png",
		params string[] tags) =>
		new(
			Id: id,
			FileName: fileName,
			FileSize: 1024,
			MimeType: mimeType,
			Url: $"https://fake.cloud.squidex.io/api/assets/{id}",
			Tags: tags.ToList(),
			Version: 1,
			Created: DateTime.UtcNow,
			LastModified: DateTime.UtcNow,
			Metadata: new AssetMetadata(PixelWidth: 800, PixelHeight: 600),
			IsProtected: false,
			FileHash: "hash-1");

	public static AssetsResponse MakeAssetsResponse(params AssetDto[] items) =>
		new(items.Length, items.ToList());

	// ── Locale factories ──────────────────────────────────────────────────────

	public static SquidexLocaleInfo MakeMasterLocale(string iso2Code = TestLocales.UkUA) =>
		new(iso2Code, IsMaster: true, IsOptional: false);

	public static SquidexLocaleInfo MakeLocale(
		string iso2Code, bool isOptional = false) =>
		new(iso2Code, IsMaster: false, IsOptional: isOptional);

	public static IReadOnlyList<SquidexLocaleInfo> MakeLocales(
		string masterLocale, params string[] otherLocales)
	{
		var list = new List<SquidexLocaleInfo> { MakeMasterLocale(masterLocale) };
		list.AddRange(otherLocales.Select(l => MakeLocale(l)));
		return list;
	}

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

	public static string AppLanguagesJson(
		string masterLocale = TestLocales.UkUA,
		params string[] otherLocales)
	{
		var items = new List<object>
		{
			new { iso2Code = masterLocale, isMaster = true,  isOptional = false }
		};

		items.AddRange(otherLocales.Select(l =>
			(object)new { iso2Code = l, isMaster = false, isOptional = false }));

		return JsonSerializer.Serialize(new { items });
	}

	public static string ResponseJson<T>(ResponseSchema<T> response) =>
		JsonSerializer.Serialize(response);

	// ── Test schemas ──────────────────────────────────────────────────────────

	public sealed class TestSchema : ISquidexSchema
	{
		public static string SchemaName => "test-schema";

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