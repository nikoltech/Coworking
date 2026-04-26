using Coworking.External.Squidex.Abstractions.Filters;

namespace Coworking.Application.Ports.Squidex.Schemas.Email;

/// <summary>
/// OData filter path constants for EmailSchema.
/// Usage: SquidexFilter.Eq(EmailPaths.Name, "welcome")
/// </summary>
public static class EmailPaths
{
    public static readonly string Name = SquidexPaths.Iv("Name");
    public static readonly string Value = SquidexPaths.Iv("Value");

    public static class OData
    {
        public static readonly string Name = SquidexPaths.ODataIv("Name");
        public static readonly string Value = SquidexPaths.ODataIv("Value");
    }
}