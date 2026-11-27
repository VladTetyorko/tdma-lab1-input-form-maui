using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class PermissionGuard(
    ICurrentUserService currentUser,
    IAlertService alerts,
    ILogger<PermissionGuard> logger) : IPermissionGuard
{
    public const string AlertTitle = "Not allowed";

    public async Task<string?> CheckAsync(Func<Role, bool> allowed, string action)
    {
        ArgumentNullException.ThrowIfNull(allowed);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        var user = currentUser.CurrentUser;
        if (user is not null && allowed(user.Role))
        {
            return null;
        }

        var message = user is null ? $"Sign in before you {action}." : $"Your role ({user.Role}) cannot {action}.";
        logger.LogWarning("Denied: {Role} (user {UserId}) cannot {Action}", currentUser.CurrentRole, user?.Id, action);
        await alerts.ShowAsync(AlertTitle, message);
        return message;
    }
}
