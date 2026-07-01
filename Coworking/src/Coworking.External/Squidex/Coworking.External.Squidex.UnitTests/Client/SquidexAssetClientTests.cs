using Coworking.External.Squidex.Abstractions.Client;
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Options;
using Coworking.External.Squidex.Auth;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Exceptions;
using Coworking.External.Squidex.UnitTests.Helpers;
using FluentAssertions;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Coworking.External.Squidex.UnitTests.Client;

public sealed class SquidexAssetClientTests
{
    private readonly MockHttpMessageHandler _mockHttp = new();
    private readonly SquidexAppOptions _options = SquidexFakes.DefaultAppOptions();

    private SquidexAssetClient CreateClient(SquidexAppOptions? options = null) =>
        new(_mockHttp.ToHttpClient(), options ?? _options, TestClientNames.Default);

    private string AssetsUrl => $"*/api/assets/{_options.AppName}";

    // ── Query ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_ReturnsFlatAssets_NotContentWrapped()
    {
        var expected = SquidexFakes.MakeAssetsResponse(
            SquidexFakes.MakeAsset("a1", "one.png"),
            SquidexFakes.MakeAsset("a2", "two.jpg", "image/jpeg"));

        _mockHttp.When(HttpMethod.Get, AssetsUrl).RespondJson(expected);

        var result = await CreateClient().QueryAsync();

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].Id.Should().Be("a1");
        result.Items[0].FileName.Should().Be("one.png");
        result.Items[1].MimeType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task QueryAsync_BuildsQueryString_SkipAlwaysSent_ValuesEscaped_TagsRepeated()
    {
        string? capturedQuery = null;
        _mockHttp.When(HttpMethod.Get, AssetsUrl).Respond(req =>
        {
            capturedQuery = req.RequestUri?.Query;
            return OkResponse(SquidexFakes.MakeAssetsResponse());
        });

        var query = AssetQuery.Create()
            .WithTop(10)
            .WithFilter("fileName eq 'Київ.png'")
            .WithFullText("hello world")
            .WithTags(["logo", "hero banner"]);

        await CreateClient().QueryAsync(query);

        capturedQuery.Should().Contain("$skip=0");     // always emitted
        capturedQuery.Should().Contain("$top=10");
        capturedQuery.Should().Contain("$filter=");
        capturedQuery.Should().Contain("%27");         // escaped quote in filter value
        capturedQuery.Should().NotContain("Київ.png"); // raw value must be encoded
        capturedQuery.Should().Contain("$search=hello%20world");
        capturedQuery.Should().Contain("tags=logo");
        capturedQuery.Should().Contain("tags=hero%20banner"); // second tag, escaped
    }

    [Fact]
    public async Task QueryAsync_StampsAppAndClient_ForAuthHandler()
    {
        // Regression: asset client must set BOTH app + client options, or the
        // multi-app auth handler resolves an empty app name and throws.
        string? capturedApp = null;
        string? capturedClient = null;

        _mockHttp.When(HttpMethod.Get, AssetsUrl).Respond(req =>
        {
            req.Options.TryGetValue(
                new HttpRequestOptionsKey<string>(SquidexAuthHandler.AppNameKey), out capturedApp);
            req.Options.TryGetValue(
                new HttpRequestOptionsKey<string>(SquidexAuthHandler.ClientNameKey), out capturedClient);
            return OkResponse(SquidexFakes.MakeAssetsResponse());
        });

        await CreateClient().QueryAsync();

        capturedApp.Should().Be(_options.AppName);
        capturedClient.Should().Be(TestClientNames.Default);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _mockHttp
            .When(HttpMethod.Get, $"*/api/assets/{_options.AppName}/missing")
            .Respond(HttpStatusCode.NotFound);

        var result = await CreateClient().GetByIdAsync("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsAsset_WhenFound()
    {
        _mockHttp
            .When(HttpMethod.Get, $"*/api/assets/{_options.AppName}/asset-1")
            .RespondJson(SquidexFakes.MakeAsset("asset-1", "kyiv.png"));

        var result = await CreateClient().GetByIdAsync("asset-1");

        result.Should().NotBeNull();
        result!.Id.Should().Be("asset-1");
        result.FileName.Should().Be("kyiv.png");
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_PostsMultipartForm_ToAssetsEndpoint()
    {
        string? contentType = null;
        _mockHttp.When(HttpMethod.Post, AssetsUrl).Respond(req =>
        {
            contentType = req.Content?.Headers.ContentType?.MediaType;
            return OkResponse(SquidexFakes.MakeAsset("new-asset"));
        });

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("file-bytes"));
        var result = await CreateClient().UploadAsync(stream, "upload.png", "image/png");

        result.Id.Should().Be("new-asset");
        contentType.Should().Be("multipart/form-data");
    }

    [Fact]
    public async Task UpdateMetadataAsync_SendsPutWithJsonBody()
    {
        string? body = null;
        _mockHttp.When(HttpMethod.Put, $"*/api/assets/{_options.AppName}/asset-1").Respond(async req =>
        {
            body = await req.Content!.ReadAsStringAsync();
            return OkResponse(SquidexFakes.MakeAsset("asset-1", "renamed.png"));
        });

        var result = await CreateClient().UpdateMetadataAsync(
            "asset-1", new UpdateAssetRequest(FileName: "renamed.png", Tags: ["logo"]));

        result.FileName.Should().Be("renamed.png");
        body.Should().Contain("renamed.png");
        body.Should().Contain("logo");
    }

    [Fact]
    public async Task DeleteAsync_AddsPermanentParam_WhenRequested()
    {
        string? capturedUrl = null;
        _mockHttp
            .When(HttpMethod.Delete, $"*/api/assets/{_options.AppName}/del-id*")
            .Respond(req =>
            {
                capturedUrl = req.RequestUri?.ToString();
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

        await CreateClient().DeleteAsync("del-id", permanent: true);

        capturedUrl.Should().Contain("permanent=true");
    }

    [Fact]
    public async Task DeleteAsync_OmitsPermanentParam_ByDefault()
    {
        string? capturedUrl = null;
        _mockHttp
            .When(HttpMethod.Delete, $"*/api/assets/{_options.AppName}/del-id*")
            .Respond(req =>
            {
                capturedUrl = req.RequestUri?.ToString();
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

        await CreateClient().DeleteAsync("del-id");

        capturedUrl.Should().NotContain("permanent");
    }

    // ── Retry (driven by configured options, not a hardcoded constant) ─────────

    [Fact]
    public async Task QueryAsync_RetriesOnTransientError_UpToConfiguredMaxAttempts()
    {
        // MaxAttempts = 2 → exactly one retry, proving the loop reads options
        // rather than the old hardwired const of 3.
        var fast = _options with { Retry = new SquidexRetryOptions { MaxAttempts = 2, BaseDelaySeconds = 0 } };

        var callCount = 0;
        _mockHttp.When(HttpMethod.Get, AssetsUrl).Respond(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        });

        var act = () => CreateClient(fast).QueryAsync();

        await act.Should().ThrowAsync<SquidexApiException>();
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task QueryAsync_RetriesThenSucceeds_OnTransientError()
    {
        var fast = _options with { Retry = new SquidexRetryOptions { MaxAttempts = 3, BaseDelaySeconds = 0 } };

        var callCount = 0;
        _mockHttp.When(HttpMethod.Get, AssetsUrl).Respond(_ =>
        {
            callCount++;
            return callCount < 3
                ? new HttpResponseMessage(HttpStatusCode.BadGateway)
                : OkResponse(SquidexFakes.MakeAssetsResponse(SquidexFakes.MakeAsset()));
        });

        var result = await CreateClient(fast).QueryAsync();

        result.Total.Should().Be(1);
        callCount.Should().Be(3);
    }

    // ── helper ──────────────────────────────────────────────────────────────

    private static HttpResponseMessage OkResponse<T>(T body) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
}
