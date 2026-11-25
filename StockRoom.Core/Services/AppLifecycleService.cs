using Microsoft.Extensions.Logging;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class AppLifecycleService(
    IAppStateService state,
    ICurrentUserService currentUser,
    IThemeService theme,
    ISessionStore session,
    IBiometricSettings biometricSettings,
    INavigationService navigation,
    ISyncService sync,
    ILogger<AppLifecycleService> logger) : IAppLifecycleService
{
    public async Task<string> RestoreAsync()
    {
        theme.Load();
        state.Restore();

        // Start listening for network changes; changes made offline in an earlier run are pushed now
        sync.Start();

        try
        {
            // Biometric unlock on: a valid session is not restored silently. The login page offers the
            // fingerprint button (or the password); the saved state (drafts, last page) waits for it
            if (biometricSettings.IsEnabled && await session.HasValidSessionAsync())
            {
                logger.LogInformation("A session exists but biometric unlock is on: waiting on the login page");
                return Routes.Login;
            }

            if (await session.LoadUserAsync() is { } user)
            {
                currentUser.SignIn(user);
                logger.LogInformation("Restored the session of user {UserId}, last page {Route}", user.Id, state.LastRoute);
                return Routes.Home;
            }
        }
        catch (Exception ex)
        {
            // Secure storage can be unavailable (reset device, restored backup): sign in again instead of crashing
            logger.LogWarning(ex, "The saved session could not be read, signing in again");
        }

        // Nobody to sign in again (first start, signed out, token expired or changed, or the user no longer exists)
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
    // restoring happens only at start (RestoreAsync). Coming back is a good moment to push queued changes
    public void OnResumed()
    {
        logger.LogInformation("Window resumed");
        sync.RequestSync();
    }

    public void OnDestroying()
    {
        logger.LogInformation("Window destroying: saving state");
        SaveState();
    }

    private void SaveState()
    {
        state.LastRoute = navigation.CurrentRoute;
        state.Save();
    }
}
