using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Exceptions;
using Coworking.External.Squidex.Localization;
using Coworking.External.Squidex.Options;
using Coworking.External.Squidex.UnitTests.Helpers;
using FluentAssertions;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Coworking.External.Squidex.UnitTests.Client;

public sealed class SquidexApiClientTests
{
    private readonly MockHttpMessageHandler _mockHttp = new();
    private readonly SquidexOptions _options = SquidexFakes.DefaultOptions();
    private readonly SquidexLocaleProvider _locales;

    public SquidexApiClientTests()
    {
        _locales = new SquidexLocaleProvider(SquidexFakes.OptionsMock(_options));
    }

    private SquidexApiClient CreateClient() =>
        new(_mockHttp.ToHttpClient(), _options, TestClientNames.Default, _locales);

    private string ContentUrl(string schema) =>
        $"*/api/content/{_options.AppName}/{schema}*";

    // ── JSON Query ────────────────────────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_ReturnsDeserializedResponse()
    {
        var expected = SquidexFakes.MakeResponse(
            SquidexFakes.MakeTestSchema("alpha"),
            SquidexFakes.MakeTestSchema("beta"));

        _mockHttp
            .When(HttpMethod.Get, ContentUrl("cities"))
            .RespondJson(expected);

        var result = await CreateClient().QueryAsync<SquidexFakes.TestSchema>(
            "cities", RequestQuery.Create().WithTake(10));

        result.Total.Should().Be(2);
        result.Items[0].Data.Name!.Value.Should().Be("alpha");
        result.Items[1].Data.Name!.Value.Should().Be("beta");
    }

    [Fact]
    public async Task QueryAsync_UsesQParam_NotODataParams()
    {
        // Ensure JSON query uses ?q= not $filter etc.
        string? capturedUrl = null;
        _mockHttp.When(HttpMethod.Get, ContentUrl("cities")).Respond(req =>
        {
            capturedUrl = req.RequestUri?.Query;
            return OkResponse(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
        });

        await CreateClient().QueryAsync<SquidexFakes.TestSchema>(
            "cities", RequestQuery.Create().WithTake(5));

        capturedUrl.Should().StartWith("?q=");
        capturedUrl.Should().NotContain("$top");
    }

    // ── OData Query ───────────────────────────────────────────────────────────

    [Fact]
    public async Task QueryODataAsync_UsesODataParams_NotQParam()
    {
        string? capturedUrl = null;
        _mockHttp.When(HttpMethod.Get, ContentUrl("cities")).Respond(req =>
        {
            capturedUrl = req.RequestUri?.Query;
            return OkResponse(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
        });

        var query = ODataQuery.Create().WithTop(10).WithFilter("data/Name/iv eq 'test'");

        await CreateClient().QueryODataAsync<SquidexFakes.TestSchema>("cities", query);

        capturedUrl.Should().Contain("$top=10");
        capturedUrl.Should().Contain("$filter=");
        capturedUrl.Should().NotContain("?q=");
    }

    [Fact]
    public async Task QueryODataAsync_EscapesFilterValue()
    {
        string? capturedUrl = null;
        _mockHttp.When(HttpMethod.Get, ContentUrl("cities")).Respond(req =>
        {
            capturedUrl = req.RequestUri?.Query;
            return OkResponse(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
        });

        var query = ODataQuery.Create().WithFilter("data/Name/iv eq 'Київ'");

        await CreateClient().QueryODataAsync<SquidexFakes.TestSchema>("cities", query);

        capturedUrl.Should().NotContain("data/Name/iv eq 'Київ'"); // raw — should be encoded
        capturedUrl.Should().Contain("%27"); // encoded quote
    }

    // ── POST Query ────────────────────────────────────────────────────────────

    [Fact]
    public async Task QueryPostAsync_PostsToQueryEndpoint()
    {
        string? capturedUrl = null;
        _mockHttp.When(HttpMethod.Post, ContentUrl("cities")).Respond(req =>
        {
            capturedUrl = req.RequestUri?.PathAndQuery;
            return OkResponse(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
        });

        await CreateClient().QueryPostAsync<SquidexFakes.TestSchema>(
            "cities", RequestQuery.Create().WithTake(5));

        capturedUrl.Should().EndWith("/query");
    }

    [Fact]
    public async Task QueryPostAsync_SendsQueryInBody_NotUrl()
    {
        string? capturedBody = null;
        _mockHttp.When(HttpMethod.Post, ContentUrl("cities")).Respond(async req =>
        {
            capturedBody = await req.Content!.ReadAsStringAsync();
            return OkResponse(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
        });

        await CreateClient().QueryPostAsync<SquidexFakes.TestSchema>(
            "cities", RequestQuery.Create().WithTake(5));

        capturedBody.Should().NotBeNullOrEmpty();
        capturedBody.Should().Contain("\"take\""); // JSON body contains query
    }

    // ── IDs query ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdsAsync_SendsSingleRequest_ForSmallBatch()
    {
        var callCount = 0;
        _mockHttp.When(HttpMethod.Get, ContentUrl("cities")).Respond(_ =>
        {
            callCount++;
            return OkResponse(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
        });

        await CreateClient().GetByIdsAsync<SquidexFakes.TestSchema>(
            "cities", ["id-1", "id-2", "id-3"]);

        callCount.Should().Be(1); // all in one request
    }

    [Fact]
    public async Task GetByIdsAsync_BatchesRequests_WhenExceedingBatchSize()
    {
        // 80 is batch size — 81 IDs = 2 requests
        var ids = Enumerable.Range(0, 81).Select(i => $"id-{i}").ToList();
        var callCount = 0;

        _mockHttp.When(HttpMethod.Get, ContentUrl("cities")).Respond(_ =>
        {
            callCount++;
            return OkResponse(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
        });

        await CreateClient().GetByIdsAsync<SquidexFakes.TestSchema>("cities", ids);

        callCount.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdsAsync_UsesIdsParam_NotQParam()
    {
        string? capturedUrl = null;
        _mockHttp.When(HttpMethod.Get, ContentUrl("cities")).Respond(req =>
        {
            capturedUrl = req.RequestUri?.Query;
            return OkResponse(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
        });

        await CreateClient().GetByIdsAsync<SquidexFakes.TestSchema>(
            "cities", ["id-1", "id-2"]);

        capturedUrl.Should().StartWith("?ids=");
    }

    // ── Headers ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_AddsXLanguagesHeader_WithSupportedLocales()
    {
        string? capturedLanguages = null;
        _mockHttp.When(HttpMethod.Get, ContentUrl("cities")).Respond(req =>
        {
            capturedLanguages = req.Headers.TryGetValues("X-Languages", out var vals)
                ? string.Join(",", vals) : null;
            return OkResponse(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
        });

        await CreateClient().QueryAsync<SquidexFakes.TestSchema>(
            "cities", RequestQuery.Create());

        capturedLanguages.Should().Be($"{TestLocales.UkUA},{TestLocales.En}");
    }

    [Fact]
    public async Task QueryAsync_AddsXFlattenAndSingleLanguage_WhenFlattenEnabled()
    {
        string? capturedFlatten = null;
        string? capturedLanguages = null;

        _mockHttp.When(HttpMethod.Get, ContentUrl("cities")).Respond(req =>
        {
            capturedFlatten = req.Headers.TryGetValues("X-Flatten", out var f) ? string.Join(",", f) : null;
            capturedLanguages = req.Headers.TryGetValues("X-Languages", out var l) ? string.Join(",", l) : null;
            return OkResponse(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
        });

        var opts = new QueryOptions { Flatten = true, Languages = [TestLocales.UkUA] };

        await CreateClient().QueryAsync<SquidexFakes.TestSchema>(
            "cities", RequestQuery.Create(), opts);

        capturedFlatten.Should().Be("true");
        capturedLanguages.Should().Be(TestLocales.UkUA);
    }

    [Fact]
    public async Task QueryAsync_AddsXUnpublishedHeader_WhenRequested()
    {
        string? capturedUnpublished = null;
        _mockHttp.When(HttpMethod.Get, ContentUrl("cities")).Respond(req =>
        {
            capturedUnpublished = req.Headers.TryGetValues("X-Unpublished", out var v)
                ? string.Join(",", v) : null;
            return OkResponse(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
        });

        await CreateClient().QueryAsync<SquidexFakes.TestSchema>(
            "cities", RequestQuery.Create(), new QueryOptions { IncludeUnpublished = true });

        capturedUnpublished.Should().Be("true");
    }

    [Fact]
    public async Task QueryAsync_AddsXNoSlowTotalHeader_WhenRequested()
    {
        string? capturedNoSlowTotal = null;
        _mockHttp.When(HttpMethod.Get, ContentUrl("cities")).Respond(req =>
        {
            capturedNoSlowTotal = req.Headers.TryGetValues("X-NoSlowTotal", out var v)
                ? string.Join(",", v) : null;
            return OkResponse(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
        });

        await CreateClient().QueryAsync<SquidexFakes.TestSchema>(
            "cities", RequestQuery.Create(), new QueryOptions { NoSlowTotal = true });

        capturedNoSlowTotal.Should().Be("true");
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _mockHttp
            .When(HttpMethod.Get, "*/api/content/test-app/cities/missing-id")
            .Respond(HttpStatusCode.NotFound);

        var result = await CreateClient()
            .GetByIdAsync<SquidexFakes.TestSchema>("cities", "missing-id");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsContent_WhenFound()
    {
        var expected = SquidexFakes.MakeContent(
            SquidexFakes.MakeTestSchema("kyiv"), "city-1");

        _mockHttp
            .When(HttpMethod.Get, "*/api/content/test-app/cities/city-1")
            .RespondJson(expected);

        var result = await CreateClient()
            .GetByIdAsync<SquidexFakes.TestSchema>("cities", "city-1");

        result.Should().NotBeNull();
        result!.Id.Should().Be("city-1");
        result.Data.Name!.Value.Should().Be("kyiv");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDraftContent()
    {
        var expected = SquidexFakes.MakeDraft(SquidexFakes.MakeTestSchema("draft"), "draft-1");

        _mockHttp
            .When(HttpMethod.Get, "*/api/content/test-app/cities/draft-1")
            .RespondJson(expected);

        var result = await CreateClient()
            .GetByIdAsync<SquidexFakes.TestSchema>("cities", "draft-1");

        result!.Status.Should().Be(TestStatuses.Draft);
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PostsWithPublishParam()
    {
        var schema = SquidexFakes.MakeTestSchema("new-city");
        var expected = SquidexFakes.MakeContent(schema, "new-id");

        _mockHttp
            .When(HttpMethod.Post, "*/api/content/test-app/cities?publish=true")
            .RespondJson(expected);

        var result = await CreateClient().CreateAsync("cities", schema, publish: true);

        result.Id.Should().Be("new-id");
    }

    [Fact]
    public async Task CreateAsync_PostsWithoutPublishParam_WhenFalse()
    {
        var schema = SquidexFakes.MakeTestSchema("draft");
        var expected = SquidexFakes.MakeDraft(schema, "draft-id");

        _mockHttp
            .When(HttpMethod.Post, "*/api/content/test-app/cities")
            .RespondJson(expected);

        var result = await CreateClient().CreateAsync("cities", schema, publish: false);

        result.Status.Should().Be(TestStatuses.Draft);
    }

    [Fact]
    public async Task DeleteAsync_SendsDeleteRequest()
    {
        _mockHttp
            .When(HttpMethod.Delete, "*/api/content/test-app/cities/del-id")
            .Respond(HttpStatusCode.NoContent);

        var act = () => CreateClient().DeleteAsync("cities", "del-id");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteAsync_AddsPermanentParam_WhenRequested()
    {
        string? capturedUrl = null;
        _mockHttp
            .When(HttpMethod.Delete, "*/api/content/test-app/cities/del-id*")
            .Respond(req =>
            {
                capturedUrl = req.RequestUri?.ToString();
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

        await CreateClient().DeleteAsync("cities", "del-id", permanent: true);

        capturedUrl.Should().Contain("permanent=true");
    }

    [Fact]
    public async Task ChangeStatusAsync_SendsPutToStatusEndpoint()
    {
        var expected = SquidexFakes.MakeContent(
            SquidexFakes.MakeTestSchema(), "id-1", TestStatuses.Archived);

        _mockHttp
            .When(HttpMethod.Put, "*/api/content/test-app/cities/id-1/status")
            .RespondJson(expected);

        var result = await CreateClient()
            .ChangeStatusAsync<SquidexFakes.TestSchema>("cities", "id-1", TestStatuses.Archived);

        result.Status.Should().Be(TestStatuses.Archived);
    }

    // ── Retry ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task QueryAsync_RetriesOnTransientError_AndSucceeds(HttpStatusCode transientCode)
    {
        var callCount = 0;
        _mockHttp.When(HttpMethod.Get, ContentUrl("cities")).Respond(_ =>
        {
            callCount++;
            return callCount < 3
                ? new HttpResponseMessage(transientCode)
                : OkResponse(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
        });

        var result = await CreateClient().QueryAsync<SquidexFakes.TestSchema>(
            "cities", RequestQuery.Create());

        result.Should().NotBeNull();
        callCount.Should().Be(3);
    }

    [Fact]
    public async Task QueryAsync_ThrowsSquidexApiException_AfterAllRetriesFail()
    {
        _mockHttp
            .When(HttpMethod.Get, ContentUrl("cities"))
            .RespondError(HttpStatusCode.InternalServerError, "Server blew up");

        var act = () => CreateClient().QueryAsync<SquidexFakes.TestSchema>(
            "cities", RequestQuery.Create());

        var ex = await act.Should().ThrowAsync<SquidexApiException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        ex.Which.Message.Should().Be("Server blew up");
    }

    [Fact]
    public async Task QueryAsync_DoesNotRetry_OnClientError()
    {
        var callCount = 0;
        _mockHttp.When(HttpMethod.Get, ContentUrl("cities")).Respond(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        });

        var act = () => CreateClient().QueryAsync<SquidexFakes.TestSchema>(
            "cities", RequestQuery.Create());

        await act.Should().ThrowAsync<SquidexApiException>();
        callCount.Should().Be(1);
    }

    // ── App Languages ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAppLocalesAsync_ReturnsFullLocaleInfo_IncludingIsMasterAndIsOptional()
    {
        _mockHttp
            .When(HttpMethod.Get, $"*/api/apps/{_options.AppName}/languages")
            .Respond(HttpStatusCode.OK,
                new StringContent(
                    SquidexFakes.AppLanguagesJson(TestLocales.UkUA, TestLocales.En),
                    Encoding.UTF8, "application/json"));

        var locales = await CreateClient().GetAppLocalesAsync();

        locales.Should().HaveCount(2);

        var master = locales.Single(l => l.IsMaster);
        master.Iso2Code.Should().Be(TestLocales.UkUA);
        master.IsOptional.Should().BeFalse();

        var secondary = locales.Single(l => !l.IsMaster);
        secondary.Iso2Code.Should().Be(TestLocales.En);
    }

    [Fact]
    public async Task GetAppLocalesAsync_IdentifiesMasterLocale()
    {
        _mockHttp
            .When(HttpMethod.Get, $"*/api/apps/{_options.AppName}/languages")
            .Respond(HttpStatusCode.OK,
                new StringContent(
                    SquidexFakes.AppLanguagesJson(
                        masterLocale: TestLocales.En, // "en" is master
                        TestLocales.UkUA),
                    Encoding.UTF8, "application/json"));

        var locales = await CreateClient().GetAppLocalesAsync();

        locales.Single(l => l.IsMaster).Iso2Code.Should().Be(TestLocales.En);
        locales.Single(l => !l.IsMaster).Iso2Code.Should().Be(TestLocales.UkUA);
    }

    // ── Private helper ────────────────────────────────────────────────────────

    private static HttpResponseMessage OkResponse<T>(T body) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
}