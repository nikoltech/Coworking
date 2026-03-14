using Coworking.Application.Common.Interfaces.HttpContext.Models;

namespace Coworking.Application.Common.Interfaces.HttpContext;

public interface IUserAccessor
{
    public ContextUserDto GetCurrentUser();
    public bool IsInRole(string role);
}