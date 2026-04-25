// Exceptions/HttpResponseExtensionsTests.cs
using Coworking.External.Squidex.Exceptions;
using FluentAssertions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Coworking.External.Squidex.UnitTests.Exceptions;

public sealed class HttpResponseExtensionsTests
{
    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.NoContent)]
    public async Task EnsureSquidexSuccessAsync_DoesNotThrow_OnSuccessCode(HttpStatusCode code)
    {
        // Arrange
        var response = new HttpResponseMessage(code);

        // Act
        var act = () => response.EnsureSquidexSuccessAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, 400)]
    [InlineData(HttpStatusCode.Unauthorized, 401)]
    [InlineData(HttpStatusCode.NotFound, 404)]
    [InlineData(HttpStatusCode.InternalServerError, 500)]
    public async Task EnsureSquidexSuccessAsync_ThrowsWithCorrectStatus_OnErrorCode(
        HttpStatusCode statusCode, int expectedCode)
    {
        // Arrange
        var body = JsonSerializer.Serialize(new
        {
            message = "Something went wrong",
            details = new[] { "detail one", "detail two" }
        });

        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        // Act
        var act = () => response.EnsureSquidexSuccessAsync(CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<SquidexApiException>();
        ((int)ex.Which.StatusCode).Should().Be(expectedCode);
        ex.Which.Message.Should().Be("Something went wrong");
        ex.Which.Details.Should().BeEquivalentTo(["detail one", "detail two"]);
    }

    [Fact]
    public async Task EnsureSquidexSuccessAsync_UsesReasonPhrase_WhenBodyIsNotJson()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            ReasonPhrase = "Bad Gateway",
            Content = new StringContent("not json at all")
        };

        // Act
        var act = () => response.EnsureSquidexSuccessAsync(CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<SquidexApiException>();
        ex.Which.Message.Should().Be("Bad Gateway");
        ex.Which.Details.Should().BeEmpty();
    }

    [Fact]
    public async Task EnsureSquidexSuccessAsync_ThrowsWithEmptyDetails_WhenNoDetailsInBody()
    {
        // Arrange
        var body = JsonSerializer.Serialize(new { message = "Error" });
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        // Act
        var act = () => response.EnsureSquidexSuccessAsync(CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<SquidexApiException>();
        ex.Which.Details.Should().BeEmpty();
    }
}