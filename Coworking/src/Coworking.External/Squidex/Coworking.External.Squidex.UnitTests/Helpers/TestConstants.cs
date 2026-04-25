using Coworking.External.Squidex.Abstractions.Localization;
using Coworking.External.Squidex.Auth;

namespace Coworking.External.Squidex.UnitTests.Helpers;

/// <summary>
/// Shared constants for tests.
/// Avoids repeated string literals across test files.
/// </summary>
internal static class TestClientNames
{
    public const string Default = SquidexAuthHandler.DefaultClient; // "Default"
    public const string Frontend = "Frontend";
    public const string Unknown = "UnknownClient";
}

internal static class TestLocales
{
    public const string UkUA = SquidexLocales.UkUA; // "uk-UA"
    public const string En = SquidexLocales.En;   // "en"
    public const string De = "de";
}

internal static class TestStatuses
{
    public const string Published = "Published";
    public const string Draft = "Draft";
    public const string Archived = "Archived";
}