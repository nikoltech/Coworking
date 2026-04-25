namespace Coworking.External.Squidex.Client;

/// <summary>
/// Named HttpClient identifiers for Squidex services.
/// Used in IHttpClientFactory registrations and CreateClient calls.
/// </summary>
public static class SquidexHttpClientNames
{
    /// <summary>Main client — includes SquidexAuthHandler in pipeline.</summary>
    public const string Api = "Squidex";

    /// <summary>Auth client — no handler, used only for token requests.</summary>
    public const string Auth = "SquidexAuth";
}