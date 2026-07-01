# Coworking.External.Squidex

Status: unit-tested (119 tests green).

A typed, performance-oriented client for the Squidex CMS built on a custom
`HttpClient`-based transport (no third-party Squidex SDK).

## Features

- Multiple apps and multiple clients (credentials) per app
- Locale synchronization (from `appsettings.json` or the Squidex app languages)
- Fluent API for building queries, filters and field paths
- Ready-to-use `SquidexSet<T>` base with CRUD/query operations; extend it for
  domain-specific repositories (via inheritance or injection)
- Separate Assets API (`ISquidexAssetClient` / `SquidexAssetSet`)
- Configurable retry with exponential backoff (`SquidexRetryOptions`)
- Configuration via `appsettings.json`

## Configuration

### appsettings.json

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

### Program.cs

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

### Locale synchronization

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

## Usage

### Helpers

```text
LocalizedField<T> - fields with localization support
IvField<T>        - invariant fields (no localization)

ISquidexContext   - main entry point for API access

RequestQuery      - JSON representation of queries
ODataQuery        - fluent OData queries

SquidexFilter     - fluent complex filters
SquidexPaths      - fluent paths to nested fields
```

### Squidex schema (DTO)

```csharp
public sealed class CitySchema
{
    [JsonPropertyName("Title")]
    public LocalizedField<string>? Title { get; set; }

    [JsonPropertyName("Synonyms")]
    public IvField<string>? Synonyms { get; set; }
}
```

### Querying data

```csharp
public class GetCitiesQueryHandler(ISquidexContext squidex)
{
    string schemaName = "city";

    // OData
    string odataPath = $"data/Title/iv";

    await squidex.Set<CitySchema>(schemaName).QueryODataAsync(
            ODataQuery.Create()
                .WithFilter($"{odataPath} eq 'test'"));

    // json
    string jsonPath = $"data.Title.iv";

    await squidex.Set<CitySchema>(schemaName).QueryAsync(RequestQuery.Create()
        .WithFilter(SquidexFilter.Eq(jsonPath, "test")));
}
```

## Extended usage

`context.Set<T>(schema)` returns a ready-to-use `ISquidexSet<T>`. For
domain-specific operations, define an interface and derive from `SquidexSet<T>`.

### Interface for a specific schema

```csharp
public interface ICityRepository : ISquidexSet<CitySchema>
{
    Task<ContentDto<CitySchema>?> GetByTitleAsync(string title, CancellationToken ct = default);
}
```

### Base implementation

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

## Assets

The Assets API is separate from schema content (different endpoint and a flat
response shape). Create a client via the factory:

```csharp
ISquidexAssetClient assets = factory.CreateAssetClientForApp("Main");

AssetsResponse page = await assets.QueryAsync(
    AssetQuery.Create().WithTop(50).WithTags(["logo"]));

AssetDto uploaded = await assets.UploadAsync(stream, "photo.png", "image/png");
await assets.UpdateMetadataAsync(uploaded.Id, new UpdateAssetRequest(Tags: ["hero"]));
await assets.DeleteAsync(uploaded.Id);
```

Extend the ready-to-use `SquidexAssetSet` base for project-specific methods:

```csharp
public sealed class MediaAssets(ISquidexAssetClient client) : SquidexAssetSet(client)
{
    // custom asset helpers...
}
```
