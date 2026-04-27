
--# Coworking.External.Squidex
Status: Need testing

-- Features
- Designed for operations with focus on performance and ease of use
- Multiple apps support
- Multiple clients support
- Locales synchronization
- Fluent API for building queries, filters and paths
- Repository pattern support with basic CRUD operations provided by base classes
- Extensible architecture for custom implementations and overrides
- Built on top of Squidex.Client library for low-level API interactions
- Configuration via appsettings.json for easy setup and management of Squidex apps and clients


-- Configuration

---- appsettings.json
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

---- Program.cs
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

Locales synchronization code. 
The simpliest way is to set in appsettings.json the list of supported locales and default locale.
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

-- Usage example


--- Usage helpers

```csharp

LocalizedField<> - for fields with localization support
IvField<> - for invariant fields without localization support


ISquidexContext - main point for API access.

RequestQuery - for json representation of queries
ODataQuery - for building OData queries in a fluent way

SquidexFilter - for building complex filters in a fluent way
SquidexPaths - for building paths to nested fields in a fluent way


```

---- Squidex schema (DTO)
```csharp
public sealed class CitySchema
{
    [JsonPropertyName("Title")]
    public LocalizedField<string>? Title { get; set; }

    [JsonPropertyName("Synonyms")]
    public IvField<string>? Synonyms { get; set; }
}
```


---- Querying data

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


-- Extended usage

---- Interfaces for processing specific schemas DTOs
```csharp
public interface ICityRepository : ISquidexRepository<CitySchema>
````

---- Repository implementation with basic CRUD operations provided by SquidexRepository base class
```csharp

public sealed class CityRepository(ISquidexApiClient client, ISquidexPaginator paginator)
    : SquidexSet<CitySchema>(client, paginator, CitySchema.SchemaName), ICityRepository


```



