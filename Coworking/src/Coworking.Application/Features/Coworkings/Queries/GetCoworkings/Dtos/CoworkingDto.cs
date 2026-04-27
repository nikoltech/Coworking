namespace Coworking.Application.Features.Coworkings.Queries.GetCoworkings.Dtos;

public record CoworkingDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public TimeOnly OpenTime { get; init; }
    public TimeOnly CloseTime { get; init; }
    public string TimeZone { get; init; } = string.Empty;
}