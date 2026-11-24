using Microsoft.Extensions.Logging;
using Plugin.Maui.Biometric;

namespace Mobile_apps_L1_Model_view.Services;

// Plugin.Maui.Biometric: BiometricPrompt on Android, Windows Hello on Windows. The only class that knows the plugin.
// Fail-soft: any platform error means "not available" and the password form stays usable
public sealed class MauiBiometricAuthenticator(ILogger<MauiBiometricAuthenticator> logger) : IBiometricAuthenticator
{
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var status = await BiometricAuthenticationService.Default.GetAuthenticationStatusAsync(AuthenticatorStrength.Strong);
            return status == BiometricHwStatus.Success;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Biometric hardware status could not be read");
            return false;
        }
    }

    public async Task<BiometricOutcome> AuthenticateAsync(string reason)
    {
        try
        {
            var request = new AuthenticationRequest
            {
                Title = "StockRoom",
                Subtitle = reason,
                NegativeText = "Use password",
                AuthStrength = AuthenticatorStrength.Strong
            };
            var response = await BiometricAuthenticationService.Default.AuthenticateAsync(request, CancellationToken.None);
            return response.Status switch
            {
                BiometricResponseStatus.Success => BiometricOutcome.Success,
                _ => BiometricOutcome.Failed
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "The biometric prompt could not be shown");
            return BiometricOutcome.NotAvailable;
        }
    }
}
