using System.Security.Cryptography;
using System.Text;

namespace Coworking.External.Squidex.Webhooks;

/// <summary>
/// Verifies the signature Squidex attaches to Rule → Webhook callbacks (the "X-Signature" header),
/// so an inbound webhook endpoint can confirm a request genuinely came from Squidex.
/// </summary>
/// <remarks>
/// Squidex computes the signature as SHA256(requestBody + sharedSecret), Base64-encoded —
/// matching the official SDK's <c>WebhookUtils.CalculateSignature</c>.
/// </remarks>
public static class SquidexWebhookSignature
{
    /// <summary>Calculates the expected signature for a given raw request body and shared secret.</summary>
    public static string CalculateSignature(string requestBody, string sharedSecret)
    {
        var bytes = Encoding.UTF8.GetBytes(requestBody + sharedSecret);
        var hash = SHA256.HashData(bytes);

        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Verifies that <paramref name="signatureHeader"/> (the "X-Signature" header value) matches
    /// the expected signature for <paramref name="requestBody"/> — using a constant-time comparison
    /// to avoid leaking the correct signature through response-timing differences.
    /// </summary>
    public static bool Verify(string requestBody, string sharedSecret, string? signatureHeader)
    {
        if (string.IsNullOrEmpty(signatureHeader))
            return false;

        var expected = CalculateSignature(requestBody, sharedSecret);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signatureHeader));
    }
}
