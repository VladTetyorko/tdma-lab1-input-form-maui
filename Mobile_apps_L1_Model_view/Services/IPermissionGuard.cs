using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// The ViewModel half of authorization. A button is hidden or disabled for a role that may not use it, but a command
// can still be invoked (a stale screen, a test, a future shortcut), so every restricted command asks this first.
// The service behind the command checks again: the service layer is the check that counts
public interface IPermissionGuard
{
    // Null when the signed-in user may do it. Otherwise the explanation for the user, which has also been shown
    // in an alert and logged as a warning; the caller then stops (and may display the text where it shows its status)
    Task<string?> CheckAsync(Func<Role, bool> allowed, string action);
}
