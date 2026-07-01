using Coworking.External.Squidex.Abstractions.Models;
using System.Text.Json.Serialization;

namespace Coworking.Application.Ports.Squidex.Schemas.City;

/// <summary>
/// Squidex "city" schema DTO.
/// Localized fields use LocalizedField — returned locales controlled by X-Languages.
/// </summary>
public sealed class CitySchema : ISquidexSchema
{
    public static string SchemaName => "city";

    [JsonPropertyName("Title")]
    public LocalizedField<string>? Title { get; set; }

    [JsonPropertyName("Synonyms")]
    public IvField<string>? Synonyms { get; set; }

    [JsonPropertyName("IsRegionCity")]
    public IvField<bool?>? IsRegionCity { get; set; }

    [JsonPropertyName("SOrder")]
    public IvField<int?>? SOrder { get; set; }

    [JsonPropertyName("PlaceId")]
    public IvField<string>? PlaceId { get; set; }
}