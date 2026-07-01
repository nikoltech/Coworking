# Coworking.External.Squidex

Status: unit-tested (120 tests green).

A typed, performance-oriented client for the Squidex CMS built on a custom
`HttpClient`-based transport (no third-party Squidex SDK).

## Features

- Multiple apps and multiple clients (credentials) per app
- Locale synchronization (from `appsettings.json` or the Squidex app languages)
- Fluent API for building queries, filters and field paths
- Ready-to-use `SquidexSet<T>` base with CRUD/query operations; extend it for
  domain-specific repositories (via inheritance or injection)
- Schema name resolved from the DTO (`ISquidexSchema`) — `context.Set<T>()` needs no string
- Separate Assets API (`ISquidexAssetClient` / `SquidexAssetSet`)
- Configurable retry with exponential backoff (`SquidexRetryOptions`)
- Configuration via `appsettings.json`

## Configuration

<details>
<summary><b>appsettings.json</b></summary>

```json
{
  "Squidex": {
    "Apps": {
      "Main": {
        "BaseUrl": "https://fake.cloud.squidex.io",
        "AppName": "my-main-app",
        "MaxPageSize": 200,
        "DefaultClient": "Default",
        "SupportedLocales": [ "uk-UA", "en" ],
        "DefaultLocale": "en",
        "Retry": {
          "MaxAttempts": 3,
          "BaseDelaySeconds": 1.0
        },
        "Clients": {
          "Default": {
            "ClientId": "my-app:default",
            "ClientSecret": "secret"
          },
          "Frontend": {
            "ClientId": "my-app:frontend",
            "ClientSecret": "secret"
          }
        }
      },
      "Blog": {
        "BaseUrl": "https://fake.cloud.squidex.io",
        "AppName": "my-blog",
        "DefaultLocale": "en",
        "Clients": {
          "Default": {
            "ClientId": "blog:default",
            "ClientSecret": "secret"
          }
        }
      }
    }
  }
}
```

</details>

<details>
<summary><b>Program.cs — registration</b></summary>

```csharp
builder.Services.AddSquidex(builder.Configuration);
builder.Services.AddSquidexContexts();

internal static IServiceCollection AddSquidexContexts(this IServiceCollection services)
{
    services.AddMainAppContexts();

    return services;
}

private static IServiceCollection AddMainAppContexts(this IServiceCollection services)
{
    const string AppName = MainSquidexContext.AppName;

    services.AddScoped<IMainSquidexContext>(sp =>
    {
        var factory = sp.GetRequiredService<SquidexClientFactory>();
        var paginator = sp.GetRequiredService<ISquidexPaginator>();
        var client = factory.CreateForApp(AppName);
        return new MainSquidexContext(client, paginator, factory);
    });

    services.AddKeyedScoped<IMainSquidexContext>(SquidexClientNames.Frontend, (sp, _) =>
    {
        var factory = sp.GetRequiredService<SquidexClientFactory>();
        var paginator = sp.GetRequiredService<ISquidexPaginator>();
        var client = factory.CreateForApp(AppName, SquidexClientNames.Frontend);
        return new MainSquidexContext(client, paginator, factory);
    });

    return services;
}
```

</details>

<details>
<summary><b>Locale synchronization</b></summary>

The simplest way is to set the supported locales and the default locale in
`appsettings.json`. Otherwise they are fetched from the Squidex app on startup:

```csharp
var app = builder.Build();

// Initialize Squidex locales once before serving requests
await InitializeSquidexLocalesAsync(app.Services, app, app.Logger);

private static async Task InitializeSquidexLocalesAsync(IServiceProvider services, WebApplication webApp, ILogger logger)
{
    logger.LogInformation("Initializing Squidex locales...");

    var localeProvider = services.GetRequiredService<SquidexLocaleProvider>();
    var squidexClientFactory = services.GetRequiredService<SquidexClientFactory>();

    var globalSqOptions = services.GetRequiredService<IOptions<SquidexGlobalOptions>>();
    var squidexApps = globalSqOptions?.Value.Apps;

    if (squidexApps is not null)
    {
        foreach (var appOptions in squidexApps)
        {
            var app = appOptions.Value;
            var appName = app.AppName;

            logger.LogInformation("Initializing locales for Squidex app '{AppName}'...", appName);

            if (app.Clients is not null && app.Clients.Count > 0)
            {
                var client = app.Clients.First();

                var squidexClient = squidexClientFactory.CreateForApp(appName, client.Value?.ClientId);
                await localeProvider.InitializeAsync(squidexClient, webApp.Lifetime.ApplicationStopping);
            }

            logger.LogInformation("Locales initialization for Squidex app '{AppName}' completed.", appName);
        }
    }

    logger.LogInformation("Squidex locales initialization completed.");
}
```

</details>

## Usage

<details>
<summary><b>Helpers</b></summary>

```text
LocalizedField<T> - fields with localization support
IvField<T>        - invariant fields (no localization)

ISquidexContext   - main entry point for API access

RequestQuery      - JSON representation of queries
ODataQuery        - fluent OData queries

SquidexFilter     - fluent complex filters
SquidexPaths      - fluent paths to nested fields
```

</details>

<details>
<summary><b>Squidex schema (DTO)</b></summary>

Implement `ISquidexSchema` so the schema name is resolved from the type
(`context.Set<CitySchema>()` — no string needed):

```csharp
public sealed class CitySchema : ISquidexSchema
{
    public static string SchemaName => "city";

    [JsonPropertyName("Title")]
    public LocalizedField<string>? Title { get; set; }

    [JsonPropertyName("Synonyms")]
    public IvField<string>? Synonyms { get; set; }
}
```

</details>

<details>
<summary><b>Querying data</b></summary>

```csharp
public class GetCitiesQueryHandler(ISquidexContext squidex)
{
    // schema resolved from CitySchema.SchemaName — no string at the call site
    // OData
    await squidex.Set<CitySchema>().QueryODataAsync(
            ODataQuery.Create().WithFilter("data/Title/iv eq 'test'"));

    // json
    await squidex.Set<CitySchema>().QueryAsync(RequestQuery.Create()
        .WithFilter(SquidexFilter.Eq("data.Title.iv", "test")));

    // explicit schema string still available (e.g. one DTO mapped to several schemas)
    await squidex.Set<CitySchema>("city").QueryAsync(RequestQuery.Create());
}
```

</details>

<details>
<summary><b>Reading results</b></summary>

`QueryAsync` returns `ResponseSchema<T>` (`Total` + `Items` of `ContentDto<T>`).
Read localized fields via `Get`/`GetLocalized`, invariant fields via `.Value`:

```csharp
ResponseSchema<CitySchema> result = await squidex.Set<CitySchema>()
    .QueryAsync(RequestQuery.Create());

foreach (ContentDto<CitySchema> item in result.Items)
{
    string? title  = item.Data.Title?.GetLocalized("uk-UA", "en"); // localized
    bool isRegion  = item.Data.IsRegionCity?.Value ?? false;        // invariant

    string id      = item.Id;       // item metadata
    int version    = item.Version;
    string status  = item.Status;
}
```

</details>

<details>
<summary><b>Filtering, sorting, paging</b></summary>

```csharp
await squidex.Set<CitySchema>().QueryAsync(
    RequestQuery.Create()
        .WithFilter(SquidexFilter.Eq(CityPaths.IsRegionCity, true))
        .WithSort([SortOption.Asc(CityPaths.SOrder)])
        .WithTake(20)
        .WithSkip(0));

// fetch every page at once (paginator uses AppOptions.MaxPageSize)
ResponseSchema<CitySchema> all = await squidex.Set<CitySchema>().GetAllAsync();

// existence check (Take=1 + NoSlowTotal applied automatically)
bool exists = await squidex.Set<CitySchema>()
    .ExistsAsync(SquidexFilter.Eq(CityPaths.PlaceId, "abc123"));
```

</details>

<details>
<summary><b>Query options</b></summary>

```csharp
// include drafts / unpublished content
await squidex.Set<CitySchema>()
    .QueryAsync(query, new QueryOptions { IncludeUnpublished = true });

// restrict returned locales (X-Languages)
await squidex.Set<CitySchema>()
    .QueryAsync(query, new QueryOptions { Languages = ["uk-UA"] });
```

> `QueryOptions.Flatten` / `QueryOptions.ForLocale(locale)` return scalar values
> instead of `IvField`/`LocalizedField` — use only with a flat DTO shape.

</details>

<details>
<summary><b>Mutations</b></summary>

```csharp
var draft = new CitySchema
{
    Title = new LocalizedField<string> { ["uk-UA"] = "Львів", ["en"] = "Lviv" },
    IsRegionCity = new IvField<bool?>(true),
};

ContentDto<CitySchema> created = await squidex.Set<CitySchema>().CreateAsync(draft, publish: true);

// optimistic concurrency via expectedVersion
await squidex.Set<CitySchema>().UpdateAsync(created.Id, draft, expectedVersion: created.Version);

await squidex.Set<CitySchema>().DeleteAsync(created.Id);
```

</details>

<details>
<summary><b>Typed context properties</b></summary>

`IMainSquidexContext` exposes named repositories as properties:

```csharp
public class GetCityHandler(IMainSquidexContext squidex)
{
    public async Task Handle(CancellationToken ct)
    {
        ContentDto<CitySchema>? kyiv = await squidex.Cities.GetByTitleAsync("Київ", ct);
        // squidex.Emails, squidex.Set<T>() and squidex.UsingClient(...) are available too
    }
}
```

</details>

<details>
<summary><b>Other client credentials</b></summary>

```csharp
// one-off, for a single call
await squidex.UsingClient(SquidexClientNames.Frontend)
             .Set<CitySchema>()
             .QueryAsync(RequestQuery.Create());

// or a separately registered keyed context (see Program.cs above)
public class PublicHandler(
    [FromKeyedServices(SquidexClientNames.Frontend)] IMainSquidexContext squidex) { }
```

</details>

## Extended usage

`context.Set<T>()` / `context.Set<T>(schema)` return a ready-to-use `ISquidexSet<T>`.
For domain-specific operations, define an interface and derive from `SquidexSet<T>`.

<details>
<summary><b>Interface for a specific schema</b></summary>

```csharp
public interface ICityRepository : ISquidexSet<CitySchema>
{
    Task<ContentDto<CitySchema>?> GetByTitleAsync(string title, CancellationToken ct = default);
}
```

</details>

<details>
<summary><b>Base implementation</b></summary>

Basic CRUD/query operations come from the `SquidexSet<T>` base class:

```csharp
public sealed class CityRepository(ISquidexApiClient client, ISquidexPaginator paginator)
    : SquidexSet<CitySchema>(client, paginator, CitySchema.SchemaName), ICityRepository
{
    public async Task<ContentDto<CitySchema>?> GetByTitleAsync(string title, CancellationToken ct = default)
    {
        var result = await QueryAsync(
            RequestQuery.Create()
                .WithTake(1)
                .WithFilter(SquidexFilter.Eq(CityPaths.Title, title)),
            ct: ct);

        return result.Items.FirstOrDefault();
    }
}
```

</details>

## Assets

The Assets API is separate from schema content (different endpoint and a flat
response shape).

<details>
<summary><b>Client operations</b></summary>

```csharp
ISquidexAssetClient assets = factory.CreateAssetClientForApp("Main");

AssetsResponse page = await assets.QueryAsync(
    AssetQuery.Create().WithTop(50).WithTags(["logo"]));

AssetDto uploaded = await assets.UploadAsync(stream, "photo.png", "image/png");
await assets.UpdateMetadataAsync(uploaded.Id, new UpdateAssetRequest(Tags: ["hero"]));
await assets.DeleteAsync(uploaded.Id);
```

</details>

<details>
<summary><b>Extending the base set</b></summary>

Extend the ready-to-use `SquidexAssetSet` base for project-specific methods:

```csharp
public sealed class MediaAssets(ISquidexAssetClient client) : SquidexAssetSet(client)
{
    // custom asset helpers...
}
```

</details>
