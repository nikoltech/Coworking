// Client/SquidexApiClientTests.cs
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
        new (_mockHttp.ToHttpClient(), _options, TestClientNames.Default, _locales);

    private string ContentUrl(string schema) =>
        $"*/api/content/{_options.AppName}/{schema}*";

    // ── Query ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_ReturnsDeserializedResponse()
    {
        // Arrange
        var expected = SquidexFakes.MakeResponse(
            SquidexFakes.MakeTestSchema("alpha"),
            SquidexFakes.MakeTestSchema("beta"));

        _mockHttp
            .When(HttpMethod.Get, ContentUrl("cities"))
            .RespondJson(expected);

        // Act
        var result = await CreateClient().QueryAsync<SquidexFakes.TestSchema>(
            "cities", RequestQuery.Create().WithTake(10));

        // Assert
        result.Total.Should().Be(2);
        result.Items[0].Data.Name!.Value.Should().Be("alpha");
        result.Items[1].Data.Name!.Value.Should().Be("beta");
    }

    [Fact]
    public async Task QueryAsync_AddsXLanguagesHeader_WithSupportedLocales()
    {
        // Arrange
        string? capturedLanguages = null;
        _mockHttp.When(HttpMethod.Get, ContentUrl("cities")).Respond(req =>
        {
            capturedLanguages = req.Headers.TryGetValues("X-Languages", out var vals)
                ? string.Join(",", vals) : null;
            return OkResponse(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
        });

        // Act
        await CreateClient().QueryAsync<SquidexFakes.TestSchema>(
            "cities", RequestQuery.Create());

        // Assert — SupportedLocales from options
        capturedLanguages.Should().Be($"{TestLocales.UkUA},{TestLocales.En}");
    }

    [Fact]
    public async Task QueryAsync_AddsXFlattenAndSingleLanguage_WhenFlattenEnabled()
    {
        // Arrange
        string? capturedFlatten = null;
        string? capturedLanguages = null;

        _mockHttp.When(HttpMethod.Get, ContentUrl("cities")).Respond(req =>
        {
            capturedFlatten = req.Headers.TryGetValues("X-Flatten", out var f) ? string.Join(",", f) : null;
            capturedLanguages = req.Headers.TryGetValues("X-Languages", out var l) ? string.Join(",", l) : null;
            return OkResponse(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
        });

        var opts = new QueryOptions { Flatten = true, Languages = [TestLocales.UkUA] };

        // Act
        await CreateClient().QueryAsync<SquidexFakes.TestSchema>(
            "cities", RequestQuery.Create(), opts);

        // Assert
        capturedFlatten.Should().Be("true");
        capturedLanguages.Should().Be(TestLocales.UkUA);
    }

    [Fact]
    public async Task QueryAsync_AddsXUnpublishedHeader_WhenRequested()
    {
        // Arrange
        string? capturedUnpublished = null;
        _mockHttp.When(HttpMethod.Get, ContentUrl("cities")).Respond(req =>
        {
            capturedUnpublished = req.Headers.TryGetValues("X-Unpublished", out var v)
                ? string.Join(",", v) : null;
            return OkResponse(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
        });

        var opts = new QueryOptions { IncludeUnpublished = true };

        // Act
        await CreateClient().QueryAsync<SquidexFakes.TestSchema>(
            "cities", RequestQuery.Create(), opts);

        // Assert
        capturedUnpublished.Should().Be("true");
    }

    [Fact]
    public async Task QueryAsync_AddsXNoSlowTotalHeader_WhenRequested()
    {
        // Arrange
        string? capturedNoSlowTotal = null;
        _mockHttp.When(HttpMethod.Get, ContentUrl("cities")).Respond(req =>
        {
            capturedNoSlowTotal = req.Headers.TryGetValues("X-NoSlowTotal", out var v)
                ? string.Join(",", v) : null;
            return OkResponse(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
        });

        var opts = new QueryOptions { NoSlowTotal = true };

        // Act
        await CreateClient().QueryAsync<SquidexFakes.TestSchema>(
            "cities", RequestQuery.Create(), opts);

        // Assert
        capturedNoSlowTotal.Should().Be("true");
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        _mockHttp
            .When(HttpMethod.Get, "*/api/content/test-app/cities/missing-id")
            .Respond(HttpStatusCode.NotFound);

        // Act
        var result = await CreateClient().GetByIdAsync<SquidexFakes.TestSchema>(
            "cities", "missing-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsContent_WhenFound()
    {
        // Arrange
        var expected = SquidexFakes.MakeContent(
            SquidexFakes.MakeTestSchema("kyiv"), "city-1");

        _mockHttp
            .When(HttpMethod.Get, "*/api/content/test-app/cities/city-1")
            .RespondJson(expected);

        // Act
        var result = await CreateClient().GetByIdAsync<SquidexFakes.TestSchema>(
            "cities", "city-1");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("city-1");
        result.Data.Name!.Value.Should().Be("kyiv");
    }

    // ── Status ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsContentWithDraftStatus()
    {
        // Arrange
        var expected = SquidexFakes.MakeDraft(
            SquidexFakes.MakeTestSchema("draft-city"), "draft-1");

        _mockHttp
            .When(HttpMethod.Get, "*/api/content/test-app/cities/draft-1")
            .RespondJson(expected);

        // Act
        var result = await CreateClient().GetByIdAsync<SquidexFakes.TestSchema>(
            "cities", "draft-1");

        // Assert
        result!.Status.Should().Be(TestStatuses.Draft);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PostsToUrlWithPublishParam_AndReturnsCreated()
    {
        // Arrange
        var schema = SquidexFakes.MakeTestSchema("new-city");
        var expected = SquidexFakes.MakeContent(schema, "new-id");

        _mockHttp
            .When(HttpMethod.Post, "*/api/content/test-app/cities?publish=true")
            .RespondJson(expected);

        // Act
        var result = await CreateClient().CreateAsync("cities", schema, publish: true);

        // Assert
        result.Id.Should().Be("new-id");
        result.Data.Name!.Value.Should().Be("new-city");
    }

    [Fact]
    public async Task CreateAsync_PostsWithoutPublishParam_WhenPublishFalse()
    {
        // Arrange
        var schema = SquidexFakes.MakeTestSchema("draft-city");
        var expected = SquidexFakes.MakeDraft(schema, "draft-id");

        _mockHttp
            .When(HttpMethod.Post, "*/api/content/test-app/cities")
            .RespondJson(expected);

        // Act
        var result = await CreateClient().CreateAsync("cities", schema, publish: false);

        // Assert
        result.Status.Should().Be(TestStatuses.Draft);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_SendsDeleteRequest_WithoutPermanentParam()
    {
        // Arrange
        _mockHttp
            .When(HttpMethod.Delete, "*/api/content/test-app/cities/del-id")
            .Respond(HttpStatusCode.NoContent);

        // Act
        var act = () => CreateClient().DeleteAsync("cities", "del-id");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteAsync_AddsPermanentParam_WhenRequested()
    {
        // Arrange
        string? capturedUrl = null;
        _mockHttp
            .When(HttpMethod.Delete, "*/api/content/test-app/cities/del-id*")
            .Respond(req =>
            {
                capturedUrl = req.RequestUri?.ToString();
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

        // Act
        await CreateClient().DeleteAsync("cities", "del-id", permanent: true);

        // Assert
        capturedUrl.Should().Contain("permanent=true");
    }

    // ── ChangeStatus ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ChangeStatusAsync_SendsPutToStatusEndpoint()
    {
        // Arrange
        var expected = SquidexFakes.MakeContent(
            SquidexFakes.MakeTestSchema(), "id-1", TestStatuses.Archived);

        _mockHttp
            .When(HttpMethod.Put, "*/api/content/test-app/cities/id-1/status")
            .RespondJson(expected);

        // Act
        var result = await CreateClient().ChangeStatusAsync<SquidexFakes.TestSchema>(
            "cities", "id-1", TestStatuses.Archived);

        // Assert
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
        // Arrange
        var callCount = 0;
        _mockHttp.When(HttpMethod.Get, ContentUrl("cities")).Respond(_ =>
        {
            callCount++;
            return callCount < 3
                ? new HttpResponseMessage(transientCode)
                : OkResponse(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
        });

        // Act
        var result = await CreateClient().QueryAsync<SquidexFakes.TestSchema>(
            "cities", RequestQuery.Create());

        // Assert
        result.Should().NotBeNull();
        callCount.Should().Be(3);
    }

    [Fact]
    public async Task QueryAsync_ThrowsSquidexApiException_AfterAllRetriesFail()
    {
        // Arrange
        _mockHttp
            .When(HttpMethod.Get, ContentUrl("cities"))
            .RespondError(HttpStatusCode.InternalServerError, "Server blew up");

        // Act
        var act = () => CreateClient().QueryAsync<SquidexFakes.TestSchema>(
            "cities", RequestQuery.Create());

        // Assert
        var ex = await act.Should().ThrowAsync<SquidexApiException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        ex.Which.Message.Should().Be("Server blew up");
    }

    [Fact]
    public async Task QueryAsync_DoesNotRetry_OnClientError()
    {
        // Arrange
        var callCount = 0;
        _mockHttp.When(HttpMethod.Get, ContentUrl("cities")).Respond(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        });

        // Act
        var act = () => CreateClient().QueryAsync<SquidexFakes.TestSchema>(
            "cities", RequestQuery.Create());

        // Assert
        await act.Should().ThrowAsync<SquidexApiException>();
        callCount.Should().Be(1); // no retry on 4xx
    }

    // ── App Languages ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAppLocalesAsync_ReturnsLocalesFromSquidex()
    {
        // Arrange
        _mockHttp
            .When(HttpMethod.Get, $"*/api/apps/{_options.AppName}/languages")
            .Respond(HttpStatusCode.OK,
                new StringContent(
                    SquidexFakes.AppLanguagesJson(TestLocales.UkUA, TestLocales.En, TestLocales.De),
                    Encoding.UTF8, "application/json"));

        // Act
        var locales = await CreateClient().GetAppLocalesAsync();

        // Assert
        locales.Should().BeEquivalentTo([TestLocales.UkUA, TestLocales.En, TestLocales.De]);
    }

    // ── Private helper ────────────────────────────────────────────────────────

    private static HttpResponseMessage OkResponse<T>(T body) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
}