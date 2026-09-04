using System.ComponentModel.DataAnnotations;

namespace Coworking.Application.Ports.Languages;

public sealed class LanguageOptions
{
    public const string SectionName = "Languages";

    [Required]
    public string Default { get; init; } = default!;

    [Required, MinLength(1)]
    public string[] Supported { get; init; } = [];
}
