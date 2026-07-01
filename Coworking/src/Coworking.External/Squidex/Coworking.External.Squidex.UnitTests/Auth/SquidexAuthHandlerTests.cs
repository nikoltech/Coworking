// Auth/SquidexAuthHandlerTests.cs
using Coworking.External.Squidex.Auth;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using RichardSzalay.MockHttp;
using System.Net;

namespace Coworking.External.Squidex.UnitTests.Auth;

// SquidexTokenService is sealed, so it can't be mocked — a real instance is built
// on top of a mocked identity-server token endpoint instead.
public sealed class SquidexAuthHandlerTests
{
    private readonly MockHttpMessageHandler _tokenBackend = new();
    private readonly SquidexTokenService _tokenService;

    public SquidexAuthHandlerTests()
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(SquidexHttpClientNames.Auth)
               .Returns(_tokenBackend.ToHttpClient());

        _tokenService = new SquidexTokenService(
            factory, new MemoryCache(new MemoryCacheOptions()), SquidexFakes.GlobalOptionsMock());
    }

    private HttpClient BuildClient(MockHttpMessageHandler mockBackend)
    {
        var handler = new SquidexAuthHandler(_tokenService)
        {
            InnerHandler = mockBackend
        };
        return new HttpClient(handler) { BaseAddress = new Uri(TestUrls.BaseUrl) };
    }

    private HttpRequestMessage MakeRequest(
        string url = "/api/content/app/cities",
        string? appName = TestApps.Default,
        string? clientName = TestClientNames.Default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (appName is not null)
            request.Options.Set(
                new HttpRequestOptionsKey<string>(SquidexAuthHandler.AppNameKey), appName);

        if (clientName is not null)
            request.Options.Set(
                new HttpRequestOptionsKey<string>(SquidexAuthHandler.ClientNameKey), clientName);

        return request;
    }

    private MockedRequest SetupTokenEndpoint(string token = "test-access-token") =>
        _tokenBackend
            .When(HttpMethod.Post, "*/identity-server/connect/token")
            .Respond(HttpStatusCode.OK,
                new System.Net.Http.StringContent(
                    SquidexFakes.TokenJson(token),
                    System.Text.Encoding.UTF8, "application/json"));

    [Fact]
    public async Task SendAsync_AttachesBearerToken_ToRequest()
    {
        // Arrange
        SetupTokenEndpoint("test-token");

        string? capturedAuth = null;
        var mockBackend = new MockHttpMessageHandler();
        mockBackend.When("*").Respond(req =>
        {
            capturedAuth = req.Headers.Authorization?.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("{}") };
        });

        // Act
        await BuildClient(mockBackend).SendAsync(MakeRequest());

        // Assert
        capturedAuth.Should().Be("Bearer test-token");
    }

    [Fact]
    public async Task SendAsync_RefreshesTokenAndRetries_On401()
    {
        // Arrange — token endpoint returns a fresh token on each call
        var tokenCallCount = 0;
        _tokenBackend
            .When(HttpMethod.Post, "*/identity-server/connect/token")
            .Respond(_ =>
            {
                tokenCallCount++;
                var token = tokenCallCount == 1 ? "stale-token" : "fresh-token";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        SquidexFakes.TokenJson(token), System.Text.Encoding.UTF8, "application/json")
                };
            });

        var callCount = 0;
        var capturedAuthHeaders = new List<string?>();
        var mockBackend = new MockHttpMessageHandler();
        mockBackend.When("*").Respond(req =>
        {
            callCount++;
            capturedAuthHeaders.Add(req.Headers.Authorization?.ToString());
            var status = callCount == 1
                ? HttpStatusCode.Unauthorized
                : HttpStatusCode.OK;
            return new HttpResponseMessage(status) { Content = new StringContent("{}") };
        });

        // Act
        var response = await BuildClient(mockBackend).SendAsync(MakeRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        callCount.Should().Be(2);
        tokenCallCount.Should().Be(2); // cache invalidated on 401 — token re-fetched
        capturedAuthHeaders.Should().Equal("Bearer stale-token", "Bearer fresh-token");
    }

    [Fact]
    public async Task SendAsync_DoesNotRetry_WhenResponseIsNot401()
    {
        // Arrange
        SetupTokenEndpoint("token");

        var callCount = 0;
        var mockBackend = new MockHttpMessageHandler();
        mockBackend.When("*").Respond(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.Forbidden)
            { Content = new StringContent("{}") };
        });

        // Act
        await BuildClient(mockBackend).SendAsync(MakeRequest());

        // Assert — no retry on 403
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_UsesDefaultClient_WhenNoClientNameInOptions()
    {
        // Arrange
        string? capturedClientId = null;
        _tokenBackend
            .When(HttpMethod.Post, "*/identity-server/connect/token")
            .Respond(req =>
            {
                var body = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                capturedClientId = body.Split('&')
                    .FirstOrDefault(p => p.StartsWith("client_id="))
                    ?.Split('=')[1];

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        SquidexFakes.TokenJson(), System.Text.Encoding.UTF8, "application/json")
                };
            });

        var mockBackend = new MockHttpMessageHandler();
        mockBackend.When("*").Respond(HttpStatusCode.OK);

        // Request without client name option
        var request = MakeRequest(clientName: null);

        // Act
        await BuildClient(mockBackend).SendAsync(request);

        // Assert — Default client credentials used
        capturedClientId.Should().Be("app%3Adefault");
    }

    [Fact]
    public async Task SendAsync_UsesFrontendClient_WhenSpecifiedInOptions()
    {
        // Arrange
        string? capturedClientId = null;
        _tokenBackend
            .When(HttpMethod.Post, "*/identity-server/connect/token")
            .Respond(req =>
            {
                var body = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                capturedClientId = body.Split('&')
                    .FirstOrDefault(p => p.StartsWith("client_id="))
                    ?.Split('=')[1];

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        SquidexFakes.TokenJson("frontend-token"), System.Text.Encoding.UTF8, "application/json")
                };
            });

        string? capturedAuth = null;
        var mockBackend = new MockHttpMessageHandler();
        mockBackend.When("*").Respond(req =>
        {
            capturedAuth = req.Headers.Authorization?.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("{}") };
        });

        // Act
        await BuildClient(mockBackend).SendAsync(MakeRequest(clientName: TestClientNames.Frontend));

        // Assert
        capturedClientId.Should().Be("app%3Afrontend");
        capturedAuth.Should().Be("Bearer frontend-token");
    }
}
