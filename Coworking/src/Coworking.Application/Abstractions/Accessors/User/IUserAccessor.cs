using Coworking.Application.Abstractions.Accessors.User.Models;

namespace Coworking.Application.Abstractions.Accessors.User;

public interface IUserAccessor
{
    public ContextUserDto GetCurrentUser();
    public bool IsInRole(string role);
}