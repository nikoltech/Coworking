namespace Coworking.Application.Ports.Squidex.Schemas.Email;

/// <summary>
/// OData filter path constants for EmailSchema.
/// Usage: SquidexFilter.Eq(EmailPaths.Name, "welcome")
/// </summary>
public static class EmailPaths
{
    private const string Root = "data";

    public const string Name = $"{Root}.Name.iv";
    public const string Value = $"{Root}.Value.iv";
}