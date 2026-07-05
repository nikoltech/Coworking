using Coworking.External.Squidex.Webhooks;
using FluentAssertions;

namespace Coworking.External.Squidex.UnitTests.Webhooks;

public sealed class SquidexWebhookSignatureTests
{
    private const string Secret = "shared-secret";
    private const string Body = "{\"type\":\"Updated\",\"id\":\"content-1\"}";

    [Fact]
    public void CalculateSignature_IsDeterministic_ForSameBodyAndSecret()
    {
        var first = SquidexWebhookSignature.CalculateSignature(Body, Secret);
        var second = SquidexWebhookSignature.CalculateSignature(Body, Secret);

        first.Should().Be(second);
    }

    [Fact]
    public void CalculateSignature_ChangesWithBody()
    {
        var original = SquidexWebhookSignature.CalculateSignature(Body, Secret);
        var changed = SquidexWebhookSignature.CalculateSignature(Body + " ", Secret);

        changed.Should().NotBe(original);
    }

    [Fact]
    public void CalculateSignature_ChangesWithSecret()
    {
        var original = SquidexWebhookSignature.CalculateSignature(Body, Secret);
        var changed = SquidexWebhookSignature.CalculateSignature(Body, "different-secret");

        changed.Should().NotBe(original);
    }

    [Fact]
    public void Verify_ReturnsTrue_ForMatchingSignature()
    {
        var signature = SquidexWebhookSignature.CalculateSignature(Body, Secret);

        SquidexWebhookSignature.Verify(Body, Secret, signature).Should().BeTrue();
    }

    [Fact]
    public void Verify_ReturnsFalse_ForTamperedBody()
    {
        var signature = SquidexWebhookSignature.CalculateSignature(Body, Secret);

        SquidexWebhookSignature.Verify(Body + "tampered", Secret, signature).Should().BeFalse();
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongSecret()
    {
        var signature = SquidexWebhookSignature.CalculateSignature(Body, Secret);

        SquidexWebhookSignature.Verify(Body, "wrong-secret", signature).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Verify_ReturnsFalse_WhenSignatureHeaderMissing(string? header)
    {
        SquidexWebhookSignature.Verify(Body, Secret, header).Should().BeFalse();
    }
}
