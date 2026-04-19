namespace Coworking.Application.Features.Desks.Queries.GetDesks.Dtos;

public record DeskDto(
    int Id,
    string Name,
    string Description
//bool IsAvailable
);