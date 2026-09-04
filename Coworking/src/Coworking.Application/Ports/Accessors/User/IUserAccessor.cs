using Coworking.Application.Ports.Accessors.User.Models;

namespace Coworking.Application.Ports.Accessors.User;

public interface IUserAccessor
{
    public ContextUserDto GetCurrentUser();
    public bool IsInRole(string role);
}