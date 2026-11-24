using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class SessionStore(
    ISecretStore secrets,
    ITokenService tokens,
    IUserRepository users,
    ILogger<SessionStore> logger) : ISessionStore
{
    public const string TokenKey = "stockroom.session-token.v1";

    public async Task SaveAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        await secrets.SetAsync(TokenKey, await tokens.IssueAsync(user.Id));
        logger.LogInformation("Session saved for user {UserId}", user.Id);
    }

    public async Task<bool> HasValidSessionAsync() => await ReadValidUserIdAsync() is not null;

    public async Task<User?> LoadUserAsync()
    {
        if (await ReadValidUserIdAsync() is not { } userId)
        {
            return null;
        }

        var user = await users.GetByIdAsync(userId);
        if (user is null)
        {
            logger.LogWarning("The saved session names user {UserId}, who no longer exists", userId);
            await ClearAsync();
        }

        return user;
    }

    public async Task ClearAsync()
    {
        await secrets.RemoveAsync(TokenKey);
        logger.LogInformation("Session cleared");
    }

    private async Task<Guid?> ReadValidUserIdAsync()
    {
        if (await secrets.GetAsync(TokenKey) is not { } token)
        {
            return null;
        }

        if (await tokens.ValidateAsync(token) is { } userId)
        {
            return userId;
        }

        // Tampered, signed with another key or expired: drop it so the next start does not try it again
        logger.LogWarning("The saved session token is not valid (changed or expired) and was removed");
        await ClearAsync();
        return null;
    }
}
