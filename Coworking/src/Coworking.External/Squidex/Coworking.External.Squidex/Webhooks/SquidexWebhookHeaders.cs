namespace Coworking.External.Squidex.Webhooks;

/// <summary>
/// Header names Squidex attaches to Rule → Webhook callbacks.
/// </summary>
public static class SquidexWebhookHeaders
{
    /// <summary>Signature computed by Squidex over the request body — see <see cref="SquidexWebhookSignature"/>.</summary>
    public const string Signature = "X-Signature";
}
