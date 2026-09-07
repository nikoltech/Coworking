# Coworking.External.Squidex

Status: unit-tested (159 tests green).

A typed client for the Squidex CMS built on a custom `HttpClient` transport, with no
third-party Squidex SDK. Narrow by design: made for a known set of schemas.

- **Covered** — multi-app, multi-client, retries, locale sync, components, assets, webhooks.
- **Out of scope** — schema management, GraphQL, bulk operations, streaming.

[Configuration](#configuration) · [Locales](#locales) · [Retries](#retries-and-deadlines) ·
[Usage](#usage) · [Components](#components) · [Assets](#assets) · [Webhooks](#webhooks) ·
[Extending](#extending)

## Configuration

```json
{
  "Squidex": {
    "DefaultApp": "Main",
    "Apps": {
      "Main": {
        "BaseUrl": "https://fake.cloud.squidex.io",
        "AppName": "my-main-app",
        "SupportedLocales": [ "uk-UA", "en" ],
        "DefaultLocale": "en",
        "Retry": { "MaxAttempts": 3 },
        "Limits": { "MaxParallelRequests": 16 },
        "DefaultClient": "Default",
        "Clients": {
          "Default":  { "ClientId": "my-app:default",  "ClientSecret": "secret" },
          "Frontend": { "ClientId": "my-app:frontend", "ClientSecret": "secret" }
        }
      }
    }
  }
}
```

| Key | Meaning |
|---|---|
| `Apps` | Map of *your* app keys → settings. `"Main"` is the id used for DI keys and `CreateForApp("Main")` — not the name in Squidex. |
| `DefaultApp` | Optional, read only when several apps are configured — see [registration](#registration). |
| `BaseUrl` | Squidex host. |
| `AppName` | The app's real name in Squidex — goes into the URL. |
| `DefaultClient` | Which `Clients` entry to use when a call names none. |
| `Clients` | Map of *your* client keys → credentials. Same idea as `Apps`: the key is yours, `ClientId` is Squidex's. |
| `DefaultLocale`, `SupportedLocales` | Both optional — see [Locales](#locales). |
| `Retry.MaxAttempts` | Cap on sends for one call: `3` means at most three. Applies only to the statuses in [Retries](#retries-and-deadlines). |
| `Limits.MaxParallelRequests` | Cap on parallel requests **inside one operation**. It does not limit how many operations you start — total load stays yours to manage. |

### Registration

```csharp
builder.Services.AddSquidex(builder.Configuration);
```

That's the whole DI setup ✅ — locales are the one thing it leaves open, see
[Locales](#locales).

How `ISquidexContext` is registered follows from the number of apps:

- **One app** → unkeyed. `ISquidexContext` injects directly.
- **Several apps** → keyed by app key (`[FromKeyedServices("Blog")]`), and **nothing is
  registered unkeyed** unless `DefaultApp` names one of them.

Only `ISquidexContext` comes from DI. `ISquidexApiClient` and `ISquidexAssetClient` are built
per call by `SquidexClientFactory` — see [Assets](#assets).

### Locales

Every query needs a locale: it sends `SupportedLocales` as `X-Languages`, or `DefaultLocale`
alone when `X-Flatten` is on — unless that call passes its own `QueryOptions.Languages`. Both
config keys are optional, so there are two ways to supply them:

- **Put them in config** and the client never asks Squidex for them.
- **Leave them out** and call `SquidexLocaleInitializer.InitializeAllAsync` at startup — it
  reads the app's real languages and fills in whichever key you left blank.

`AddSquidex` does not call the initializer: it is an opt-in helper for keeping locales in sync
with the CMS, and where — or whether — to run it is yours to decide. Until it has run and
succeeded, anything the config did not supply stays unresolved, and reading it throws rather
than guessing a locale.

The call always contacts Squidex, config or not — that is what it is for. If it cannot reach
the app, or the master locale contradicts a configured `DefaultLocale`, the locales are left
unresolved and reading them throws.

### Retries and deadlines

Retried statuses: **408, 429, 500, 502, 503, 504** — nothing else. A 400 or a 409 comes
straight back, and so does a connection failure or a timeout: those surface as exceptions,
which are never retried. `MaxAttempts` caps sends only along the status path above.

The pause is `Retry-After` when the server sends one, otherwise it doubles from one second;
both are jittered so parallel requests do not all return at once.

There is no total-timeout setting: one app-wide number cannot serve both a user-facing
endpoint and a background sync. Pass a deadline instead — it bounds the requests, the retry
pauses and the parallel batches alike:

```csharp
using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
cts.CancelAfter(TimeSpan.FromSeconds(3));

var faq = await context.Set<Faq>("faq").GetAllAsync(ct: cts.Token);
```

## Usage

Give a schema DTO a name via `ISquidexSchema` so `Set<T>()` needs no schema string:

```csharp
public sealed class CitySchema : ISquidexSchema
{
    public static string SchemaName => "city";

    [JsonPropertyName("Title")]
    public LocalizedField<string>? Title { get; set; }

    [JsonPropertyName("IsRegionCity")]
    public IvField<bool?>? IsRegionCity { get; set; }
}
```

Inject `ISquidexContext` and ask it for the set:

```csharp
public class GetCitiesHandler(ISquidexContext squidex)
{
    public async Task Handle(CancellationToken ct)
    {
        var set = squidex.Set<CitySchema>();
        // ...
    }
}
```

**Reading.** Filter, sort and page a query, then pull values out of the fields:

```csharp
var page = await set.QueryAsync(
    RequestQuery.Create()
        .WithFilter(SquidexFilter.Eq(CityPaths.IsRegionCity, true))
        .WithSort([SortOption.Asc(CityPaths.SOrder)])
        .WithTake(20),
    ct: ct);

var title = page.Items[0].Data.Title?.GetLocalized("uk-UA", "en"); // localized
var region = page.Items[0].Data.IsRegionCity?.Value ?? false;      // invariant
```

Every page at once, or a cheap existence check:

```csharp
var all = await set.GetAllAsync(ct: ct);

var exists = await set.ExistsAsync(
    SquidexFilter.Eq(CityPaths.PlaceId, "abc"),
    ct: ct);
```

**Writing.** `UpdateAsync` and `PatchAsync` take an optional `expectedVersion` — the ETag, for
optimistic concurrency:

```csharp
var created = await set.CreateAsync(
    new CitySchema { IsRegionCity = new IvField<bool?>(true) },
    ct: ct);

await set.UpdateAsync(
    created.Id,
    created.Data,
    expectedVersion: created.Version,
    ct: ct);

await set.DeleteAsync(created.Id, ct: ct);
```

**Another client's credentials**, for one call:

```csharp
await squidex.UsingClient("Frontend")
    .Set<CitySchema>()
    .QueryAsync(RequestQuery.Create(), ct: ct);
```

Three ways to shape a request:

- `RequestQuery` + `SquidexFilter` + `SquidexPaths` — JSON queries, used by `QueryAsync`.
- `ODataQuery` — the fluent OData alternative, used by `QueryODataAsync`.
- `QueryOptions` — per-call headers: `X-Languages`, `X-Unpublished`, `X-NoSlowTotal`,
  `X-Flatten`.

### Components

A `Components` field arrives as a partitioned list. One component type needs nothing special:

```csharp
[JsonPropertyName("Blocks")]
public IvField<List<TextBlock>>? Blocks { get; set; }
```

Several types need a discriminator. Squidex sends `schemaId`, but its value differs per
environment — so add a stable field of your own to the component schemas (`componentType`
below) and name it on the base class:

```csharp
[SquidexComponent("componentType")]                // field carrying the discriminator
[SquidexComponentType("hero", typeof(HeroBlock))]  // value → type
[SquidexComponentType("cta", typeof(CtaBlock))]
public abstract class PageBlock
{
    [JsonPropertyName("schemaId")]
    public string? SchemaId { get; set; }
}

public sealed class HeroBlock : PageBlock
{
    [JsonPropertyName("heading")]
    public string? Heading { get; set; }
}

public sealed class CtaBlock : PageBlock
{
    [JsonPropertyName("label")]
    public string? Label { get; set; }
}

public sealed class PageSchema : ISquidexSchema
{
    public static string SchemaName => "page";

    [JsonPropertyName("Blocks")]
    public IvField<List<PageBlock>>? Blocks { get; set; }
}
```

What Squidex sends — note `schemaId` sits ahead of the discriminator:

```json
{
  "Blocks": {
    "iv": [
      { "schemaId": "guid-1", "componentType": "hero", "heading": "About" },
      { "schemaId": "guid-2", "componentType": "cta",  "label": "Buy" }
    ]
  }
}
```

```csharp
foreach (var block in page?.Data?.Blocks?.Value ?? [])
{
    var text = block switch
    {
        HeroBlock h => h.Heading,
        CtaBlock c => c.Label,
        _ => null
    };
}
```

That leading `schemaId` is why `System.Text.Json` polymorphism cannot be used here — it only
reads a discriminator that is the *first* property.

Nothing to register: `SquidexComponentAttribute` is a `JsonConverterAttribute`. The
discriminator is written back on save, since it is a real field of the component schema. A
value with no matching `[SquidexComponentType]`, or a component missing the field altogether,
throws `JsonException` — an unknown component is never silently dropped.

### Custom set methods

`Set<T>()` is ready to use as-is. For extra methods on a schema, derive from `SquidexSet<T>`: a
subclass calls the inherited query methods, and `Client`, `Paginator` and `Schema` are
`protected` when it needs to go lower:

```csharp
public interface ICityRepository : ISquidexSet<CitySchema>
{
    Task<ContentDto<CitySchema>?> GetByTitleAsync(
        string title,
        CancellationToken ct = default);
}

public sealed class CityRepository(ISquidexApiClient client, ISquidexPaginator paginator)
    : SquidexSet<CitySchema>(client, paginator, CitySchema.SchemaName), ICityRepository
{
    public async Task<ContentDto<CitySchema>?> GetByTitleAsync(
        string title,
        CancellationToken ct = default)
    {
        var query = RequestQuery.Create()
            .WithTake(1)
            .WithFilter(SquidexFilter.Eq(CityPaths.Title, title));

        return (await QueryAsync(query, ct: ct)).Items.FirstOrDefault();
    }
}
```

`SquidexContext` is open to subclassing on the same terms — how you then group these types on
your side is yours to decide.

## Assets

Separate API — different endpoint, flat response shape, no schema. Not in DI: ask the factory
for a client per app (and optionally per client).

```csharp
ISquidexAssetClient assets = factory.CreateAssetClientForApp("Main");

var page = await assets.QueryAsync(
    AssetQuery.Create().WithTop(50).WithTags(["logo"]));

var uploaded = await assets.UploadAsync(stream, "photo.png", "image/png");

await assets.UpdateMetadataAsync(
    uploaded.Id,
    new UpdateAssetRequest(Tags: ["hero"]));

await assets.DeleteAsync(uploaded.Id);
```

Extend `SquidexAssetSet` the same way as `SquidexSet<T>` for project-specific asset methods.

## Webhooks

Squidex Rules call an HTTP endpoint on content/asset changes. Split by dependency so the
library itself stays free of ASP.NET Core hosting:

- `SquidexWebhookSignature` (main library) — verifies `X-Signature`.
- `SquidexContentWebhookEvent` / `SquidexAssetWebhookEvent` / `SquidexWebhookEventKind`
  (`Abstractions.Webhooks.Events`) — typed payloads, visible from `Application` too.

```csharp
app.MapPost("/webhooks/squidex", async (
    HttpRequest request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var body = await new StreamReader(request.Body).ReadToEndAsync(ct);
    var signature = request.Headers[SquidexWebhookHeaders.Signature];

    if (!SquidexWebhookSignature.Verify(body, sharedSecret, signature))
        return Results.Unauthorized();

    var json = JsonDocument.Parse(body).RootElement;

    if (SquidexWebhookEventClassifier.Classify(json) == SquidexWebhookEventKind.Content)
    {
        var content = json.Deserialize<SquidexContentWebhookEvent>()!;
        await mediator.Publish(new SquidexContentChanged(content), ct);
    }

    return Results.Ok();
});
```

## Extending

The library's named `HttpClient` is public, so its pipeline is open for your own handlers —
logging, tracing, extra headers:

```csharp
services.AddSquidex(configuration);

services.AddHttpClient(SquidexHttpClientNames.Api)
    .AddHttpMessageHandler<MyHandler>();
```
