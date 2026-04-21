using Coworking.Application.Abstractions.HttpContext.Models;

namespace Coworking.Application.Abstractions.HttpContext;

public interface IUserAccessor
{
    public ContextUserDto GetCurrentUser();
    public bool IsInRole(string role);
}