using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Abstractions.Models;

public sealed record ContentDto<T>(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("created")] DateTime Created,
    [property: JsonPropertyName("lastModified")] DateTime LastModified,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("data")] T Data);