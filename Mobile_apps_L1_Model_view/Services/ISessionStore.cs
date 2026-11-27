using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// Keeps the signed-in user across app restarts: one session token in ISecretStore.
// The saved thing is the token, not a user id, so a changed or expired value cannot sign anyone in
public interface ISessionStore
{
    // Issues a token for the user and stores it (replacing the previous one)
    Task SaveAsync(User user);

    // True when a stored token is still valid (does not sign anyone in)
    Task<bool> HasValidSessionAsync();

    // The user named by the stored token; null when there is none, it is invalid or expired
    // (an invalid token is removed) or the user no longer exists
    Task<User?> LoadUserAsync();

    // Sign-out: removes the token
    Task ClearAsync();
}
