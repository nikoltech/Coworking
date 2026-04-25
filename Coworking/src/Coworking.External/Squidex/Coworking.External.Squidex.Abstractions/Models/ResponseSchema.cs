using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Coworking.External.Squidex.Abstractions.Models;

public sealed record ResponseSchema<T>(
    [property: JsonPropertyName("total")] long Total,
    [property: JsonPropertyName("items")] List<ContentDto<T>> Items);