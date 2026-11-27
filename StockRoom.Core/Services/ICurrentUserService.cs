using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// Who is acting right now: the owner of everything they create
public interface ICurrentUserService
{
    User? CurrentUser { get; }

    // Role of the signed-in user; null when nobody is signed in
    Role? CurrentRole { get; }

    bool IsInRole(Role role);

    // Raised after SignIn and SignOut, for things that live longer than a page (header, menu)
    event EventHandler? Changed;

    void SignIn(User user);

    void SignOut();
}
