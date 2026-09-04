namespace Coworking.Application.Ports.Languages;

public interface ILanguageProvider
{
    /// <summary>
    /// Two-letter ISO code; "uk" when the request carries none.
    /// </summary>
    string CurrentLanguage { get; }
}
