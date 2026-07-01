// Auth/SquidexTokenServiceTests.cs
using Coworking.External.Squidex.Abstractions.Options;
using Coworking.External.Squidex.Auth;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Exceptions;
using Coworking.External.Squidex.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text;

namespace Coworking.External.Squidex.UnitTests.Auth;

public sealed class SquidexTokenServiceTests
{
    private readonly MockHttpMessageHandler _mockHttp = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private SquidexTokenService CreateService(SquidexAppOptions? options = null)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(SquidexHttpClientNames.Auth)
               .Returns(_mockHttp.ToHttpClient());

        return new SquidexTokenService(
            factory, _cache, SquidexFakes.GlobalOptionsMock(options));
    }

    private MockedRequest SetupTokenEndpoint(
        string token = "test-access-token",
        int expiresIn = 3600) =>
        _mockHttp
            .When(HttpMethod.Post, "*/identity-server/connect/token")
            .Respond(HttpStatusCode.OK,
                new StringContent(
                    SquidexFakes.TokenJson(token, expiresIn),
                    Encoding.UTF8, "application/json"));

    [Fact]
    public async Task GetTokenAsync_ReturnsToken_WhenRequestSucceeds()
    {
        // Arrange
        SetupTokenEndpoint("my-token");
        var service = CreateService();

        // Act
        var token = await service.GetTokenAsync(TestApps.Default, TestClientNames.Default, CancellationToken.None);

        // Assert
        token.Should().Be("my-token");
    }

    [Fact]
    public async Task GetTokenAsync_ReturnsCachedToken_OnSecondCall()
    {
        // Arrange
        var callCount = 0;
        _mockHttp
            .When(HttpMethod.Post, "*/identity-server/connect/token")
            .Respond(_ =>
            {
                Interlocked.Increment(ref callCount);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        SquidexFakes.TokenJson(), Encoding.UTF8, "application/json")
                };
            });

        var service = CreateService();

        // Act
        await service.GetTokenAsync(TestApps.Default, TestClientNames.Default, CancellationToken.None);
        await service.GetTokenAsync(TestApps.Default, TestClientNames.Default, CancellationToken.None);

        // Assert — only one HTTP call
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTokenAsync_Throws_WhenClientNotConfigured()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = () => service.GetTokenAsync(TestApps.Default, TestClientNames.Unknown, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{TestClientNames.Unknown}*");
    }

    [Fact]
    public async Task GetTokenAsync_Throws_WhenAppNotConfigured()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = () => service.GetTokenAsync(TestApps.Unknown, TestClientNames.Default, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{TestApps.Unknown}*");
    }

    [Fact]
    public async Task GetTokenAsync_Throws_WhenSquidexReturns401()
    {
        // Arrange
        _mockHttp
            .When(HttpMethod.Post, "*/identity-server/connect/token")
            .RespondError(HttpStatusCode.Unauthorized, "Invalid credentials");

        var service = CreateService();

        // Act
        var act = () => service.GetTokenAsync(TestApps.Default, TestClientNames.Default, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SquidexApiException>()
            .Where(e => e.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InvalidateToken_ClearsCache_SoNextCallFetchesFresh()
    {
        // Arrange
        var callCount = 0;
        _mockHttp
            .When(HttpMethod.Post, "*/identity-server/connect/token")
            .Respond(_ =>
            {
                callCount++;
                var token = callCount == 1 ? "token-v1" : "token-v2";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        SquidexFakes.TokenJson(token), Encoding.UTF8, "application/json")
                };
            });

        var service = CreateService();

        await service.GetTokenAsync(TestApps.Default, TestClientNames.Default, CancellationToken.None);

        // Act
        service.InvalidateToken(TestApps.Default, TestClientNames.Default);
        var freshToken = await service.GetTokenAsync(TestApps.Default, TestClientNames.Default, CancellationToken.None);

        // Assert
        freshToken.Should().Be("token-v2");
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task GetTokenAsync_IsConcurrencySafe_FetchesTokenOnlyOnce()
    {
        // Arrange
        var callCount = 0;
        _mockHttp
            .When(HttpMethod.Post, "*/identity-server/connect/token")
            .Respond(_ =>
            {
                Interlocked.Increment(ref callCount);
                Thread.Sleep(10); // simulate latency
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        SquidexFakes.TokenJson(), Encoding.UTF8, "application/json")
                };
            });

        var service = CreateService();

        // Act — 20 concurrent requests
        await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => service.GetTokenAsync(TestApps.Default, TestClientNames.Default, CancellationToken.None)));

        // Assert — only one actual HTTP call despite concurrency
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTokenAsync_UsesFrontendCredentials_ForFrontendClient()
    {
        // Arrange
        string? capturedClientId = null;
        _mockHttp
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
                        SquidexFakes.TokenJson(), Encoding.UTF8, "application/json")
                };
            });

        var service = CreateService();

        // Act
        await service.GetTokenAsync(TestApps.Default, TestClientNames.Frontend, CancellationToken.None);

        // Assert — correct credentials used for Frontend client
        capturedClientId.Should().Be("app%3Afrontend"); // URL-encoded "app:frontend"
    }
}
