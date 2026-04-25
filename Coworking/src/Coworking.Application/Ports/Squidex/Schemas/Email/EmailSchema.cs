using Coworking.External.Squidex.Abstractions.Models;
using System.Text.Json.Serialization;

namespace Coworking.Application.Ports.Squidex.Schemas.Email;

public sealed class EmailSchema
{
    [JsonPropertyName("Name")] public IvField<string>? Name { get; set; }
    [JsonPropertyName("Value")] public IvField<string>? Value { get; set; }
}