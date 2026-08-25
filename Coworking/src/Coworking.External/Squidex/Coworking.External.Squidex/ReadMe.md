# Coworking.External.Squidex

Status: unit-tested (139 tests green).

A typed client for the Squidex CMS built on a custom `HttpClient` transport (no third-party
Squidex SDK). Multi-app, multi-client, retry, and locale sync are first-class; schema
management, GraphQL, bulk ops and streaming are out of scope — this is a narrow client for a
known set of schemas, not a full Squidex SDK.

## Configuration

```json
{
  "Squidex": {
    "DefaultApp": "Main",
    "Apps": {
      "Main": {
        "BaseUrl": "https://fake.cloud.squidex.io",
        "AppName": "my-main-app",
        "DefaultClient": "Default",
        "SupportedLocales": [ "uk-UA", "en" ],
        "DefaultLocale": "en",
        "Retry": { "MaxAttempts": 3 },
        "Limits": { "MaxParallelRequests": 16 },
        "Clients": {
          "Default":  { "ClientId": "my-app:default",  "ClientSecret": "secret" },
          "Frontend": { "ClientId": "my-app:frontend", "ClientSecret": "secret" }
        }
      }
    }
  }
}
```

```csharp
builder.Services.AddSquidex(builder.Configuration);
```

That's the whole setup. `ISquidexContext` is then ready to inject:
- **One app configured** → `ISquidexContext` is registered unkeyed.
- **Several apps** → registered keyed by app name (`[FromKeyedServices("Blog")]`); set
  `DefaultApp` if one of them should *also* be available unkeyed.

### Load and retries

`Limits.MaxParallelRequests` caps how many requests a **single** operation fans out into —
paging a schema with `GetAllAsync`, or splitting an id list in `GetByIdsAsync`. It does not
limit how many operations you start: total load stays yours to manage.

The named client stays open for your own handlers:

```csharp
services.AddHttpClient(SquidexHttpClientNames.Api).AddHttpMessageHandler(...);
```

`Retry.MaxAttempts` counts requests, not extra ones — `3` means at most three sends. A
`Retry-After` header is obeyed when the server sends one; otherwise the pause grows
exponentially from one second. Either way it is spread randomly, so parallel requests do not
all come back at once.

There is no option for how long a call may take in total, because one app-wide number cannot
serve both a user-facing endpoint and a background sync. Pass a deadline instead — it bounds
the requests, the retry pauses and the parallel batches alike:

```csharp
using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
cts.CancelAfter(TimeSpan.FromSeconds(3));

var faq = await context.Set<Faq>("faq").GetAllAsync(ct: cts.Token);
```

A generous deadline waits out a long `Retry-After` and succeeds; a tight one gives up. Both
fall out of the caller's own budget, with nothing to configure here.

> **Removed:** `Retry.BaseDelaySeconds`. The backoff now starts from a fixed one second, and
> how long a call may wait is the caller's deadline rather than a setting.

`DefaultLocale`/`SupportedLocales` can be omitted — they're then fetched from the Squidex app
on startup (see `SquidexLocaleProvider.InitializeAsync`).

## Usage

Give a schema DTO a name via `ISquidexSchema` so `Set<T>()` needs no schema string:

```csharp
public sealed class CitySchema : ISquidexSchema
{
    public static string SchemaName => "city";

    [JsonPropertyName("Title")] public LocalizedField<string>? Title { get; set; }
    [JsonPropertyName("IsRegionCity")] public IvField<bool?>? IsRegionCity { get; set; }
}
```

```csharp
public class GetCitiesHandler(ISquidexContext squidex)
{
    public async Task Handle(CancellationToken ct)
    {
        var set = squidex.Set<CitySchema>();

        // query — filter, sort, page
        var page = await set.QueryAsync(
            RequestQuery.Create()
                .WithFilter(SquidexFilter.Eq(CityPaths.IsRegionCity, true))
                .WithSort([SortOption.Asc(CityPaths.SOrder)])
                .WithTake(20), ct: ct);

        var title = page.Items[0].Data.Title?.GetLocalized("uk-UA", "en"); // localized field
        var region = page.Items[0].Data.IsRegionCity?.Value ?? false;       // invariant field

        // every page at once, or a cheap existence check
        var all = await set.GetAllAsync(ct: ct);
        var exists = await set.ExistsAsync(SquidexFilter.Eq(CityPaths.PlaceId, "abc"), ct: ct);

        // mutate — Update/Patch take an optional expectedVersion for optimistic concurrency (ETag)
        var created = await set.CreateAsync(new CitySchema { IsRegionCity = new IvField<bool?>(true) }, ct: ct);
        await set.UpdateAsync(created.Id, created.Data, expectedVersion: created.Version, ct: ct);
        await set.DeleteAsync(created.Id, ct: ct);

        // a different client's credentials, one-off
        await squidex.UsingClient(SquidexClientNames.Frontend).Set<CitySchema>().QueryAsync(RequestQuery.Create());
    }
}
```

`RequestQuery`/`SquidexFilter`/`SquidexPaths` build JSON queries; `ODataQuery` is the fluent
alternative for OData (`QueryODataAsync`). `QueryOptions` controls `X-Languages`,
`X-Unpublished`, `X-NoSlowTotal`, `X-Flatten` per call.

### Components

A `Components` field arrives as a partitioned list. One component type needs nothing special:

```csharp
[JsonPropertyName("Blocks")] public IvField<List<TextBlock>>? Blocks { get; set; }
```

Several types need a discriminator. Squidex sends `schemaId`, but it differs per environment,
so name a stable field of your own — `System.Text.Json` polymorphism cannot be used here,
as it only reads a discriminator that is the first property:

```csharp
[SquidexComponent("componentType")]
[SquidexComponentType("hero", typeof(HeroBlock))]
[SquidexComponentType("cta", typeof(CtaBlock))]
public abstract class PageBlock
{
    [JsonPropertyName("schemaId")] public string? SchemaId { get; set; }
}
```

```csharp
foreach (var block in page?.Data?.Blocks?.Value ?? [])
{
    var text = block switch { HeroBlock h => h.Heading, CtaBlock c => c.Label, _ => null };
}
```

Nothing to register — the attribute is a `JsonConverterAttribute`. The discriminator is written
back on save, since it is a real field of the component schema.

### Domain-specific repositories

`Set<T>()` is ready to use as-is. For extra methods on a schema, derive from `SquidexSet<T>`:

```csharp
public interface ICityRepository : ISquidexSet<CitySchema>
{
    Task<ContentDto<CitySchema>?> GetByTitleAsync(string title, CancellationToken ct = default);
}

public sealed class CityRepository(ISquidexApiClient client, ISquidexPaginator paginator)
    : SquidexSet<CitySchema>(client, paginator, CitySchema.SchemaName), ICityRepository
{
    public async Task<ContentDto<CitySchema>?> GetByTitleAsync(string title, CancellationToken ct = default) =>
        (await QueryAsync(RequestQuery.Create().WithTake(1)
            .WithFilter(SquidexFilter.Eq(CityPaths.Title, title)), ct: ct)).Items.FirstOrDefault();
}
```

Expose repositories as typed properties by subclassing `SquidexContext` (see
`MainSquidexContext` for a full example) — optional, only needed for the `.Cities`-style
shortcut on top of the DI-provided `ISquidexContext`.

## Assets

Separate API — different endpoint, flat response shape, no schema.

```csharp
ISquidexAssetClient assets = factory.CreateAssetClientForApp("Main");

var page = await assets.QueryAsync(AssetQuery.Create().WithTop(50).WithTags(["logo"]));
var uploaded = await assets.UploadAsync(stream, "photo.png", "image/png");
await assets.UpdateMetadataAsync(uploaded.Id, new UpdateAssetRequest(Tags: ["hero"]));
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
app.MapPost("/webhooks/squidex", async (HttpRequest request, IMediator mediator, CancellationToken ct) =>
{
    var body = await new StreamReader(request.Body).ReadToEndAsync(ct);
    if (!SquidexWebhookSignature.Verify(body, sharedSecret, request.Headers[SquidexWebhookHeaders.Signature]))
        return Results.Unauthorized();

    var json = JsonDocument.Parse(body).RootElement;

    if (SquidexWebhookEventClassifier.Classify(json) == SquidexWebhookEventKind.Content)
        await mediator.Publish(new SquidexContentChanged(json.Deserialize<SquidexContentWebhookEvent>()!), ct);

    return Results.Ok();
});
```
