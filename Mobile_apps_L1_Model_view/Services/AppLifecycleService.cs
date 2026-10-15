using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Data;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class AppLifecycleService(
    IAppStateService state,
    ICurrentUserService currentUser,
    IUserRepository users,
    INavigationService navigation,
    ILogger<AppLifecycleService> logger) : IAppLifecycleService
{
    public async Task<string> RestoreAsync()
    {
        state.Restore();

        if (state.SignedInUserId is { } userId && await users.GetByIdAsync(userId) is { } user)
        {
            currentUser.SignIn(user);
            logger.LogInformation("Restored the session of {User}, last page {Route}", user.FullName, state.LastRoute);
            return Routes.Products;
        }

        // Nobody to sign in again (first start, signed out, or the user no longer exists)
        state.Clear();
        logger.LogInformation("No session to restore");
        return Routes.Login;
    }

    public void OnCreated() => logger.LogInformation("Window created");

    public void OnActivated() => logger.LogInformation("Window activated");

    public void OnDeactivated() => logger.LogInformation("Window deactivated");

    public void OnStopped()
    {
        logger.LogInformation("Window stopped: saving state");
        SaveState();
    }

    // The process was alive the whole time, so the in-memory state is still correct:
    // restoring happens only at start (RestoreAsync)
    public void OnResumed() => logger.LogInformation("Window resumed");

    public void OnDestroying()
    {
        logger.LogInformation("Window destroying: saving state");
        SaveState();
    }

    private void SaveState()
    {
        state.SignedInUserId = currentUser.CurrentUser?.Id;
        state.LastRoute = navigation.CurrentRoute;
        state.Save();
    }
}
