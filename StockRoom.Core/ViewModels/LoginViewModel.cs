using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

public partial class LoginViewModel(
    IAuthService auth,
    ICurrentUserService currentUser,
    ISessionStore session,
    IBiometricSettings biometricSettings,
    IBiometricAuthenticator biometrics,
    INavigationService navigation,
    ILogger<LoginViewModel> logger) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    public partial string Login { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    public partial string Password { get; set; } = string.Empty;

    // Why the last attempt failed, or empty
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => ErrorMessage.Length > 0;

    // The fingerprint button: a valid saved session, "Biometric unlock" on, and hardware that can check
    [ObservableProperty]
    public partial bool IsBiometricAvailable { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    [NotifyCanExecuteChangedFor(nameof(BiometricSignInCommand))]
    public partial bool IsBusy { get; set; }

    public void OnAppearing() => LoadCommand.Execute(null);

    [RelayCommand]
    private async Task LoadAsync()
    {
        currentUser.SignOut();
        Password = string.Empty;
        ErrorMessage = string.Empty;
        IsBiometricAvailable = await CanOfferBiometricAsync();
    }

    [RelayCommand(CanExecute = nameof(CanSignIn))]
    private async Task SignInAsync()
    {
        IsBusy = true;
        try
        {
            var result = await auth.LoginAsync(Login, Password);
            Password = string.Empty;
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error!;
                return;
            }

            await StartSessionAsync(result.Value!, saveSession: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSignIn() => !IsBusy && Login.Trim().Length > 0 && Password.Length > 0;

    [RelayCommand(CanExecute = nameof(CanBiometricSignIn))]
    private async Task BiometricSignInAsync()
    {
        IsBusy = true;
        try
        {
            var outcome = await biometrics.AuthenticateAsync("Unlock StockRoom");
            if (outcome != BiometricOutcome.Success)
            {
                logger.LogWarning("Biometric unlock did not succeed: {Outcome}", outcome);
                ErrorMessage = outcome switch
                {
                    BiometricOutcome.NotAvailable => "Biometric unlock is not available now. Use your password.",
                    _ => "The biometric check did not succeed (failed or cancelled). Use your password."
                };
                return;
            }

            // The biometric check only proves who holds the device: who they are comes from the signed token
            var user = await LoadSavedUserAsync();
            if (user is null)
            {
                logger.LogWarning("Biometric unlock succeeded but there is no valid saved session");
                IsBiometricAvailable = false;
                ErrorMessage = "Your saved session is no longer valid. Sign in with your password.";
                return;
            }

            logger.LogInformation("User {UserId} unlocked the app with biometrics", user.Id);
            await StartSessionAsync(user, saveSession: false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanBiometricSignIn() => !IsBusy && IsBiometricAvailable;

    partial void OnIsBiometricAvailableChanged(bool value) => BiometricSignInCommand.NotifyCanExecuteChanged();

    private async Task StartSessionAsync(User user, bool saveSession)
    {
        currentUser.SignIn(user);
        ErrorMessage = string.Empty;
        if (saveSession)
        {
            try
            {
                await session.SaveAsync(user);
            }
            catch (Exception ex)
            {
                // The user is signed in for this run; only the "stay signed in after a restart" part is lost
                logger.LogWarning(ex, "The session of user {UserId} could not be saved", user.Id);
            }
        }

        await navigation.GoToAsync(Routes.Absolute(Routes.Home));
    }

    private async Task<User?> LoadSavedUserAsync()
    {
        try
        {
            return await session.LoadUserAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "The saved session could not be read");
            return null;
        }
    }

    private async Task<bool> CanOfferBiometricAsync()
    {
        try
        {
            return biometricSettings.IsEnabled && await session.HasValidSessionAsync() && await biometrics.IsAvailableAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Biometric unlock could not be checked");
            return false;
        }
    }
}
