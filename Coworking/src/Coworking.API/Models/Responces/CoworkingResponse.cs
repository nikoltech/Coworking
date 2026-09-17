using Swashbuckle.AspNetCore.Annotations;

namespace Coworking.API.Models.Responces;

public record CoworkingResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;

    [SwaggerSchema("True when the coworking never closes; openTime and closeTime are meaningless then.")]
    public bool IsNonStop { get; init; }

    // kept non-null for clients that predate isNonStop: they read 00:00–00:00 as non-stop
    [SwaggerSchema("Opening time. 00:00 for non-stop coworkings; check isNonStop instead.")]
    public TimeOnly OpenTime { get; init; }

    [SwaggerSchema("Closing time. 00:00 for non-stop coworkings; check isNonStop instead.")]
    public TimeOnly CloseTime { get; init; }

    public string TimeZone { get; init; } = string.Empty;
    public int SlotSizeMinutes { get; init; }
}
