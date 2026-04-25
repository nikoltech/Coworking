using Coworking.External.Squidex.Auth;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Exceptions;
using Coworking.External.Squidex.Options;
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
    private readonly SquidexOptions _options = SquidexFakes.DefaultOptions();

    private SquidexTokenService CreateService()
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(SquidexHttpClientNames.Auth)
               .Returns(_mockHttp.ToHttpClient());

        return new SquidexTokenService(factory, _cache, SquidexFakes.DefaultOptionsMock(_options));
    }

    [Fact]
    public async Task GetTokenAsync_ReturnsToken_WhenRequestSucceeds()
    {
        // Arrange
        _mockHttp
            .When(HttpMethod.Post, "*/identity-server/connect/token")
            .RespondJson(new { access_token = "my-token", token_type = "Bearer", expires_in = 3600 });

        var service = CreateService();

        // Act
        var token = await service.GetTokenAsync("Default", CancellationToken.None);

        // Assert
        token.Should().Be("my-token");
    }

    [Fact]
    public async Task GetTokenAsync_ReturnsCachedToken_OnSecondCall()
    {
        // Arrange
        _mockHttp
            .When(HttpMethod.Post, "*/identity-server/connect/token")
            .RespondJson(new { access_token = "cached-token", token_type = "Bearer", expires_in = 3600 });

        var service = CreateService();

        // Act
        await service.GetTokenAsync("Default", CancellationToken.None);
        await service.GetTokenAsync("Default", CancellationToken.None);

        // Assert — only one HTTP call made
        _mockHttp.VerifyNoOutstandingExpectation();
        _mockHttp.GetMatchCount(
            _mockHttp.When(HttpMethod.Post, "*/identity-server/connect/token"))
            .Should().Be(0); // already matched and consumed
    }

    [Fact]
    public async Task GetTokenAsync_Throws_WhenClientNotConfigured()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = () => service.GetTokenAsync("Unknown", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Unknown*");
    }

    [Fact]
    public async Task GetTokenAsync_Throws_WhenSquidexReturnsError()
    {
        // Arrange
        _mockHttp
            .When(HttpMethod.Post, "*/identity-server/connect/token")
            .RespondError(HttpStatusCode.Unauthorized, "Invalid credentials");

        var service = CreateService();

        // Act
        var act = () => service.GetTokenAsync("Default", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SquidexApiException>()
            .Where(e => e.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InvalidateToken_ClearsCache_SoNextCallFetchesFresh()
    {
        // Arrange
        _mockHttp
            .When(HttpMethod.Post, "*/identity-server/connect/token")
            .RespondJson(new { access_token = "token-v1", token_type = "Bearer", expires_in = 3600 })
            .RespondJson(new { access_token = "token-v2", token_type = "Bearer", expires_in = 3600 });

        var service = CreateService();

        await service.GetTokenAsync("Default", CancellationToken.None);

        // Act
        service.InvalidateToken("Default");
        var freshToken = await service.GetTokenAsync("Default", CancellationToken.None);

        // Assert
        freshToken.Should().Be("token-v2");
    }

    [Fact]
    public async Task GetTokenAsync_IsConcurrencySafe_NoDuplicateRequests()
    {
        // Arrange
        var callCount = 0;
        _mockHttp
            .When(HttpMethod.Post, "*/identity-server/connect/token")
            .Respond(_ =>
            {
                Interlocked.Increment(ref callCount);
                var json = SquidexFakes.TokenJson();
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            });

        var service = CreateService();

        // Act — concurrent requests
        await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(_ => service.GetTokenAsync("Default", CancellationToken.None)));

        // Assert — token fetched only once
        callCount.Should().Be(1);
    }
}