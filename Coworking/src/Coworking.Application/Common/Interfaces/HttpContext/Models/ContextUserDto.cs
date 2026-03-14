namespace Coworking.Application.Common.Interfaces.HttpContext.Models;

public class ContextUserDto
{
    public string Name { get; set; }

    public string EmailAddress { get; set; }

    public string ProfileImage { get; set; }

    public bool IsAuthenticated { get; set; }

    public int? CurrentUserId { get; set; }
}