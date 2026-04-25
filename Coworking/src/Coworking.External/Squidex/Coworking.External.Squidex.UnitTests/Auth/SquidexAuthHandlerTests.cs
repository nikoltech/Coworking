using Coworking.External.Squidex.Auth;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using RichardSzalay.MockHttp;
using System.Net;

namespace Coworking.External.Squidex.UnitTests.Auth;

public sealed class SquidexAuthHandlerTests
{
    private readonly MockHttpMessageHandler _mockHttp = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private SquidexTokenService CreateTokenService()
    {
        var factory = Substitute.For<IHttpClientFactory>();

        // Redirecting internal auth requests to our MockHttp handler
        factory.CreateClient(SquidexHttpClientNames.Auth)
               .Returns(_mockHttp.ToHttpClient());

        return new SquidexTokenService(
            factory,
            _cache,
            SquidexFakes.DefaultOptionsMock());
    }

    private HttpClient BuildClient(SquidexTokenService tokenService)
    {
        var handler = new SquidexAuthHandler(tokenService)
        {
            InnerHandler = _mockHttp
        };

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://cloud.squidex.io")
        };
    }

    [Fact]
    public async Task SendAsync_AttachesBearerToken_ToRequest()
    {
        // Arrange
        var tokenService = CreateTokenService();
        var client = BuildClient(tokenService);

        // 1. Setup mock for the token issuance (identity server)
        _mockHttp.Expect(HttpMethod.Post, "*/identity-server/connect/token")
            .RespondJson(new { access_token = "valid-token", expires_in = 3600 });

        // 2. Setup mock for the actual API call, verifying the header
        _mockHttp.Expect(HttpMethod.Get, "*/api/content/app/cities")
            .WithHeaders("Authorization", "Bearer valid-token")
            .Respond(HttpStatusCode.OK);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/content/app/cities");
        request.Options.Set(new HttpRequestOptionsKey<string>(SquidexAuthHandler.ClientNameKey), "Default");

        // Act
        await client.SendAsync(request);

        // Assert
        _mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task SendAsync_RefreshesTokenAndRetries_On401()
    {
        // Arrange
        var tokenService = CreateTokenService();
        var client = BuildClient(tokenService);

        // First call sequence
        _mockHttp.Expect(HttpMethod.Post, "*/identity-server/connect/token")
            .RespondJson(new { access_token = "stale-token", expires_in = 3600 });

        _mockHttp.Expect(HttpMethod.Get, "*/api/content/app/cities")
            .WithHeaders("Authorization", "Bearer stale-token")
            .Respond(HttpStatusCode.Unauthorized);

        // Retry sequence after 401
        _mockHttp.Expect(HttpMethod.Post, "*/identity-server/connect/token")
            .RespondJson(new { access_token = "fresh-token", expires_in = 3600 });

        _mockHttp.Expect(HttpMethod.Get, "*/api/content/app/cities")
            .WithHeaders("Authorization", "Bearer fresh-token")
            .Respond(HttpStatusCode.OK);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/content/app/cities");
        request.Options.Set(new HttpRequestOptionsKey<string>(SquidexAuthHandler.ClientNameKey), "Default");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task SendAsync_UsesDefaultClient_WhenNoClientNameInOptions()
    {
        // Arrange
        var tokenService = CreateTokenService();
        var client = BuildClient(tokenService);

        // Verify that the call is made for the "Default" client credentials
        _mockHttp.Expect(HttpMethod.Post, "*/identity-server/connect/token")
            .With(req => req.Content!.ReadAsStringAsync().Result.Contains("client_id=app:default"))
            .RespondJson(new { access_token = "default-token", expires_in = 3600 });

        _mockHttp.Expect(HttpMethod.Get, "*/test")
            .Respond(HttpStatusCode.OK);

        var request = new HttpRequestMessage(HttpMethod.Get, "/test");

        // Act
        await client.SendAsync(request);

        // Assert
        _mockHttp.VerifyNoOutstandingExpectation();
    }
}