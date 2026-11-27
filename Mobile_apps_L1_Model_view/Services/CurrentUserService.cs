using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    public User? CurrentUser { get; private set; }

    public Role? CurrentRole => CurrentUser?.Role;

    public bool IsInRole(Role role) => CurrentUser?.Role == role;

    public void SignIn(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        CurrentUser = user;
    }

    public void SignOut()
    {
        CurrentUser = null;
    }
}
