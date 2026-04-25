using Coworking.External.Squidex.Abstractions.Localization;
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

namespace Coworking.External.Squidex.UnitTests.Client;

public sealed class SquidexApiClientTests
{
    private readonly MockHttpMessageHandler _mockHttp = new();
    private readonly SquidexOptions _options = SquidexFakes.DefaultOptions();
    private readonly SquidexLocaleProvider _locales;

    public SquidexApiClientTests()
    {
        _locales = new SquidexLocaleProvider(SquidexFakes.DefaultOptionsMock(_options));
    }

    private SquidexApiClient CreateClient() =>
        new(_mockHttp.ToHttpClient(), _options, "Default", _locales);

    // ── Query ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_ReturnsDeserializedResponse()
    {
        // Arrange
        var expected = SquidexFakes.MakeResponse(
            new SquidexFakes.TestSchema(Name: new IvField<string>("alpha")),
            new SquidexFakes.TestSchema(Name: new IvField<string>("beta")));

        _mockHttp
            .When(HttpMethod.Get, "*/api/content/test-app/test-schema*")
            .RespondJson(expected);

        var client = CreateClient();

        // Act
        var result = await client.QueryAsync<SquidexFakes.TestSchema>(
            "test-schema",
            RequestQuery.Create().WithTake(10));

        // Assert
        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].Data.Name!.Value.Should().Be("alpha");
    }

    [Fact]
    public async Task QueryAsync_AddsXLanguagesHeader_WithSupportedLocales()
    {
        // Arrange
        string? capturedLanguages = null;
        _mockHttp.When(HttpMethod.Get, "*/api/content/*").Respond(req =>
        {
            capturedLanguages = req.Headers.TryGetValues("X-Languages", out var vals)
                ? string.Join(",", vals)
                : null;
            var json = JsonSerializer.Serialize(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var client = CreateClient();

        // Act
        await client.QueryAsync<SquidexFakes.TestSchema>(
            "test-schema", RequestQuery.Create());

        // Assert
        capturedLanguages.Should().Be("uk-UA,en");
    }

    [Fact]
    public async Task QueryAsync_AddsXFlattenAndXLanguages_WhenFlattenEnabled()
    {
        // Arrange
        string? capturedFlatten = null;
        string? capturedLanguages = null;

        _mockHttp.When(HttpMethod.Get, "*/api/content/*").Respond(req =>
        {
            capturedFlatten = req.Headers.TryGetValues("X-Flatten", out var f)
                ? string.Join(",", f) : null;
            capturedLanguages = req.Headers.TryGetValues("X-Languages", out var l)
                ? string.Join(",", l) : null;

            var json = JsonSerializer.Serialize(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var client = CreateClient();
        var options = new QueryOptions { Flatten = true, Languages = [SquidexLocales.UkUA] };

        // Act
        await client.QueryAsync<SquidexFakes.TestSchema>(
            "test-schema", RequestQuery.Create(), options);

        // Assert
        capturedFlatten.Should().Be("true");
        capturedLanguages.Should().Be("uk-UA");
    }

    [Fact]
    public async Task QueryAsync_AddsXUnpublishedHeader_WhenRequested()
    {
        // Arrange
        string? capturedUnpublished = null;
        _mockHttp.When(HttpMethod.Get, "*/api/content/*").Respond(req =>
        {
            capturedUnpublished = req.Headers.TryGetValues("X-Unpublished", out var v)
                ? string.Join(",", v) : null;
            var json = JsonSerializer.Serialize(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var client = CreateClient();
        var options = new QueryOptions { IncludeUnpublished = true };

        // Act
        await client.QueryAsync<SquidexFakes.TestSchema>(
            "test-schema", RequestQuery.Create(), options);

        // Assert
        capturedUnpublished.Should().Be("true");
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        _mockHttp
            .When(HttpMethod.Get, "*/api/content/test-app/cities/missing-id")
            .Respond(HttpStatusCode.NotFound);

        var client = CreateClient();

        // Act
        var result = await client.GetByIdAsync<SquidexFakes.TestSchema>("cities", "missing-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsContent_WhenFound()
    {
        // Arrange
        var expected = SquidexFakes.MakeContent(
            new SquidexFakes.TestSchema(Name: new IvField<string>("city")), "id-1");

        _mockHttp
            .When(HttpMethod.Get, "*/api/content/test-app/cities/id-1")
            .RespondJson(expected);

        var client = CreateClient();

        // Act
        var result = await client.GetByIdAsync<SquidexFakes.TestSchema>("cities", "id-1");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("id-1");
        result.Data.Name!.Value.Should().Be("city");
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PostsToCorrectUrl_AndReturnsCreated()
    {
        // Arrange
        var data = new SquidexFakes.TestSchema(Name: new IvField<string>("new"));
        var expected = SquidexFakes.MakeContent(data, "new-id");

        _mockHttp
            .When(HttpMethod.Post, "*/api/content/test-app/cities?publish=true")
            .RespondJson(expected);

        var client = CreateClient();

        // Act
        var result = await client.CreateAsync("cities", data, publish: true);

        // Assert
        result.Id.Should().Be("new-id");
        result.Data.Name!.Value.Should().Be("new");
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_SendsDeleteRequest()
    {
        // Arrange
        _mockHttp
            .When(HttpMethod.Delete, "*/api/content/test-app/cities/del-id")
            .Respond(HttpStatusCode.NoContent);

        var client = CreateClient();

        // Act
        var act = () => client.DeleteAsync("cities", "del-id");

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ── Retry ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_RetriesOnTransientError_AndSucceeds()
    {
        // Arrange
        var callCount = 0;
        _mockHttp.When(HttpMethod.Get, "*/api/content/*").Respond(_ =>
        {
            callCount++;
            if (callCount < 3)
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

            var json = JsonSerializer.Serialize(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var client = CreateClient();

        // Act
        var result = await client.QueryAsync<SquidexFakes.TestSchema>(
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
            .When(HttpMethod.Get, "*/api/content/*")
            .RespondError(HttpStatusCode.InternalServerError, "Server error");

        var client = CreateClient();

        // Act
        var act = () => client.QueryAsync<SquidexFakes.TestSchema>(
            "cities", RequestQuery.Create());

        // Assert
        await act.Should().ThrowAsync<SquidexApiException>()
            .Where(e => e.StatusCode == HttpStatusCode.InternalServerError);
    }

    // ── App Languages ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAppLocalesAsync_ReturnsLocales()
    {
        // Arrange
        _mockHttp
            .When(HttpMethod.Get, "*/api/apps/test-app/languages")
            .Respond(HttpStatusCode.OK,
                new StringContent(
                    SquidexFakes.AppLanguagesJson("uk-UA", "en"),
                    Encoding.UTF8, "application/json"));

        var client = CreateClient();

        // Act
        var locales = await client.GetAppLocalesAsync();

        // Assert
        locales.Should().BeEquivalentTo(["uk-UA", "en"]);
    }
}