namespace Coworking.API.Models.Requests;

public record GetDeskAvailabilityRequest(int DeskId, DateOnly Date);