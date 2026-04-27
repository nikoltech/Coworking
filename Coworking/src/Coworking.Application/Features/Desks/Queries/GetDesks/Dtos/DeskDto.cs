namespace Coworking.Application.Features.Desks.Queries.GetDesks.Dtos;

public record DeskDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    // public bool IsAvailable { get; init; } // Раскомментируй, если нужно
}