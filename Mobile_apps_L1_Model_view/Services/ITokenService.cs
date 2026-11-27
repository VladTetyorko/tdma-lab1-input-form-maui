namespace Mobile_apps_L1_Model_view.Services;

// Signed, expiring proof that a user signed in on this device. Not a server token: only this app can check it
public interface ITokenService
{
    Task<string> IssueAsync(Guid userId);

    // The user id inside the token; null when the token is malformed, changed, signed with another key or expired
    Task<Guid?> ValidateAsync(string token);
}
