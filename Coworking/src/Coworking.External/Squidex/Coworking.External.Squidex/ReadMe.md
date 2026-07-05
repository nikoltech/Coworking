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
        "Retry": { "MaxAttempts": 3, "BaseDelaySeconds": 1.0 },
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
    if (!SquidexWebhookSignature.Verify(body, sharedSecret, request.Headers["X-Signature"]))
        return Results.Unauthorized();

    var json = JsonDocument.Parse(body).RootElement;

    if (SquidexWebhookEventClassifier.Classify(json) == SquidexWebhookEventKind.Content)
        await mediator.Publish(new SquidexContentChanged(json.Deserialize<SquidexContentWebhookEvent>()!), ct);

    return Results.Ok();
});
```
