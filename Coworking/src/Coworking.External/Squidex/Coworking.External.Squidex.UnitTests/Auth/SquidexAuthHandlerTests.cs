// Auth/SquidexAuthHandlerTests.cs
using Coworking.External.Squidex.Auth;
using Coworking.External.Squidex.UnitTests.Helpers;
using FluentAssertions;
using NSubstitute;
using RichardSzalay.MockHttp;
using System.Net;

namespace Coworking.External.Squidex.UnitTests.Auth;

public sealed class SquidexAuthHandlerTests
{
    private readonly ISquidexTokenService _tokenService = Substitute.For<ISquidexTokenService>();

    private HttpClient BuildClient(MockHttpMessageHandler mockBackend)
    {
        var handler = new SquidexAuthHandler(_tokenService)
        {
            InnerHandler = mockBackend
        };
        return new HttpClient(handler) { BaseAddress = new Uri("https://cloud.squidex.io") };
    }

    private HttpRequestMessage MakeRequest(
        string url = "/api/content/app/cities",
        string clientName = TestClientNames.Default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Options.Set(
            new HttpRequestOptionsKey<string>(SquidexAuthHandler.ClientNameKey),
            clientName);
        return request;
    }

    [Fact]
    public async Task SendAsync_AttachesBearerToken_ToRequest()
    {
        // Arrange
        _tokenService.GetTokenAsync(TestClientNames.Default, Arg.Any<CancellationToken>())
            .Returns("test-token");

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
        // Arrange
        var callCount = 0;
        _tokenService.GetTokenAsync(TestClientNames.Default, Arg.Any<CancellationToken>())
            .Returns("stale-token", "fresh-token");

        var mockBackend = new MockHttpMessageHandler();
        mockBackend.When("*").Respond(_ =>
        {
            callCount++;
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
        _tokenService.Received(1).InvalidateToken(TestClientNames.Default);
    }

    [Fact]
    public async Task SendAsync_DoesNotRetry_WhenResponseIsNot401()
    {
        // Arrange
        _tokenService.GetTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("token");

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
        _tokenService.DidNotReceive().InvalidateToken(Arg.Any<string>());
    }

    [Fact]
    public async Task SendAsync_UsesDefaultClient_WhenNoClientNameInOptions()
    {
        // Arrange
        _tokenService.GetTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("token");

        string? capturedClientName = null;
        _tokenService.When(x =>
                x.GetTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(x => capturedClientName = x.Arg<string>());

        var mockBackend = new MockHttpMessageHandler();
        mockBackend.When("*").Respond(HttpStatusCode.OK);

        // Request without client name option
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");

        // Act
        await BuildClient(mockBackend).SendAsync(request);

        // Assert
        capturedClientName.Should().Be(SquidexAuthHandler.DefaultClient);
    }

    [Fact]
    public async Task SendAsync_UsesFrontendClient_WhenSpecifiedInOptions()
    {
        // Arrange
        _tokenService.GetTokenAsync(TestClientNames.Frontend, Arg.Any<CancellationToken>())
            .Returns("frontend-token");

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
        capturedAuth.Should().Be("Bearer frontend-token");
    }
}