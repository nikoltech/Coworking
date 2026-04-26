namespace Coworking.Application.Abstractions.Languages;

public interface ILanguageProvider
{
    // TODO: Consider using some more structured type for languages instead of a string.
    /// <summary>
    /// Unknown concrete code style
    /// </summary>
    string CurrentLanguage { get; }
}
