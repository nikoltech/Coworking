using Coworking.External.Squidex.Exceptions;
using FluentAssertions;
using System.Net;
using System.Text;

namespace Coworking.External.Squidex.UnitTests.Exceptions;

public sealed class HttpResponseExtensionsTests
{
    [Fact]
    public async Task EnsureSquidexSuccessAsync_DoesNotThrow_OnSuccess()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        var act = () => response.EnsureSquidexSuccessAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task EnsureSquidexSuccessAsync_ThrowsSquidexApiException_OnError(
        HttpStatusCode statusCode)
    {
        // Arrange
        var body = JsonSerializer.Serialize(new { message = "Error occurred", details = new[] { "detail1" } });
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        // Act
        var act = () => response.EnsureSquidexSuccessAsync(CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<SquidexApiException>();
        ex.Which.StatusCode.Should().Be(statusCode);
        ex.Which.Message.Should().Be("Error occurred");
        ex.Which.Details.Should().Contain("detail1");
    }

    [Fact]
    public async Task EnsureSquidexSuccessAsync_UsesReasonPhrase_WhenBodyIsNotJson()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            ReasonPhrase = "Bad Gateway",
            Content = new StringContent("not json")
        };

        // Act
        var act = () => response.EnsureSquidexSuccessAsync(CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<SquidexApiException>();
        ex.Which.Message.Should().Be("Bad Gateway");
    }
}