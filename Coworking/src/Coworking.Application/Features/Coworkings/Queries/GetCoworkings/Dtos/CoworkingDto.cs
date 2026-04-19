namespace Coworking.Application.Features.Coworkings.Queries.GetCoworkings.Dtos;

public record CoworkingDto(
    int Id,
    string Name,
    string Address,
    TimeOnly OpenTime,
    TimeOnly CloseTime,
    string TimeZone);
