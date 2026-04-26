
-- Configuration

---- appsettings.json
```json
{
  "Squidex": {
    "BaseUrl": "https://cloud.squidex.io",
    "AppName": "my-app",
    "MaxPageSize": 200,
    "DefaultLocale": "uk-UA",
    "SupportedLocales": [ "uk-UA", "en" ],
    "Clients": {
      "Default": {
        "ClientId": "my-app:default",
        "ClientSecret": "secret"
      },
      "Frontend": {
        "ClientId": "my-app:frontend",
        "ClientSecret": "secret"
      }
      ...
    }
  }
}
```

---- Program.cs
```csharp

builder.Services.AddSquidex(builder.Configuration);
builder.Services.AddSquidexSchemas(builder.Configuration);

IServiceCollection AddSquidexSchemas(this IServiceCollection services)
{
    // Default Squidex clients for all schema repositories
    services.AddScoped(sp => sp.GetRequiredService<SquidexClientFactory>().Create());

    services.AddScoped(sp => sp.GetRequiredService<SquidexClientFactory>().CreateAssetClient());

    // custom repositories for specific schemas
    services.AddScoped<ICityRepository, CityRepository>();
    services.AddScoped<IEmailRepository, EmailRepository>();

    return services;
}

...

var app = builder.Build();

// Initialize Squidex locales once before serving requests
await InitializeSquidexLocalesAsync(app);

async Task InitializeSquidexLocalesAsync(WebApplication webApp)
{
    var services = webApp.Services;
    var localeProvider = services.GetRequiredService<SquidexLocaleProvider>();
    var squidexClient = services.GetRequiredService<SquidexClientFactory>().Create();
    await localeProvider.InitializeAsync(squidexClient, webApp.Lifetime.ApplicationStopping);
}


```

-- Usage example

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

---- Interfaces for processing specific schemas DTOs
```csharp
public interface ICityRepository : ISquidexRepository<CitySchema>;
````

---- Repository implementation with basic CRUD operations provided by SquidexRepository base class
```csharp

public sealed class CityRepository(SquidexApiClient client, SquidexPaginator paginator)
    : SquidexRepository<CitySchema>(client, paginator, "city"), ICityRepository;

```

