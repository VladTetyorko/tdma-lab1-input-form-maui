using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// Checks a login and password against the local accounts. It does not start the session:
// the caller signs the returned user in (ICurrentUserService) and saves the session (ISessionStore)
public interface IAuthService
{
    // Failure carries the message for the user. An unknown login and a wrong password give the same message;
    // after too many failures the login is locked for a short time
    Task<Result<User>> LoginAsync(string login, string password);
}
