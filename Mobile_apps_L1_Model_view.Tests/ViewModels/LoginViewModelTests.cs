using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class LoginViewModelTests
{
    private const string Password = "Correct#Horse1";

    private readonly TestStore _store = new();
    private readonly Pbkdf2PasswordHasher _hasher = new(iterations: 10);
    private readonly FakeBiometricAuthenticator _biometrics = new();
    private readonly FakeBiometricSettings _biometricSettings = new();
    private readonly LoginViewModel _viewModel;
    private readonly User _storekeeper;

    public LoginViewModelTests()
    {
        var auth = new AuthService(_store.Users, _hasher, _store.Clock, _store.Logger<AuthService>());
        _viewModel = new LoginViewModel(auth, _store.CurrentUser, _store.Session, _biometricSettings, _biometrics,
            _store.Navigation, _store.Logger<LoginViewModel>());
        _storekeeper = new User
        {
            FullName = "Petro Bondarenko", Role = Role.Storekeeper, Login = "petro", PasswordHash = _hasher.Hash(Password)
        };
        _store.Users.AddAsync(_storekeeper).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Load_SignsOutAndClearsTheForm()
    {
        _store.CurrentUser.SignIn(_storekeeper);
        _viewModel.Password = "left over";
        _viewModel.ErrorMessage = "old error";

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Null(_store.CurrentUser.CurrentUser);
        Assert.Equal(string.Empty, _viewModel.Password);
        Assert.False(_viewModel.HasError);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("petro", "")]
    [InlineData("", Password)]
    [InlineData("   ", Password)]
    public void SignIn_IsDisabledUnlessBothFieldsAreFilled(string login, string password)
    {
        _viewModel.Login = login;
        _viewModel.Password = password;

        Assert.False(_viewModel.SignInCommand.CanExecute(null));
    }

    [Fact]
    public void SignIn_IsEnabledWhenBothFieldsAreFilled()
    {
        _viewModel.Login = "petro";
        _viewModel.Password = Password;

        Assert.True(_viewModel.SignInCommand.CanExecute(null));
    }

    [Fact]
    public async Task SignIn_CorrectCredentials_SignsInSavesTheSessionAndNavigatesToProducts()
    {
        _viewModel.Login = "petro";
        _viewModel.Password = Password;

        await _viewModel.SignInCommand.ExecuteAsync(null);

        Assert.Same(_storekeeper, _store.CurrentUser.CurrentUser);
        Assert.Equal(Role.Storekeeper, _store.CurrentUser.CurrentRole);
        Assert.Equal(_storekeeper.Id, (await _store.Session.LoadUserAsync())?.Id);
        Assert.Equal(Routes.Absolute(Routes.Products), Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.False(_viewModel.HasError);
    }

    [Fact]
    public async Task SignIn_ClearsThePasswordFromTheViewModel()
    {
        _viewModel.Login = "petro";
        _viewModel.Password = Password;

        await _viewModel.SignInCommand.ExecuteAsync(null);

        Assert.Equal(string.Empty, _viewModel.Password);
    }

    [Fact]
    public async Task SignIn_WrongPassword_ShowsTheErrorAndStaysSignedOut()
    {
        _viewModel.Login = "petro";
        _viewModel.Password = "wrong";

        await _viewModel.SignInCommand.ExecuteAsync(null);

        Assert.Equal(AuthService.InvalidCredentialsMessage, _viewModel.ErrorMessage);
        Assert.True(_viewModel.HasError);
        Assert.Null(_store.CurrentUser.CurrentUser);
        Assert.Empty(_store.Navigation.RequestedRoutes);
        Assert.False(_store.Secrets.Values.ContainsKey(SessionStore.TokenKey));
        Assert.Equal(string.Empty, _viewModel.Password);
    }

    [Fact]
    public async Task SignIn_UnknownLogin_ShowsTheSameError()
    {
        _viewModel.Login = "nobody";
        _viewModel.Password = Password;

        await _viewModel.SignInCommand.ExecuteAsync(null);

        Assert.Equal(AuthService.InvalidCredentialsMessage, _viewModel.ErrorMessage);
    }

    [Fact]
    public async Task SignIn_AfterTooManyFailures_ShowsTheLockoutMessage()
    {
        for (var i = 0; i < AuthService.MaxFailedAttempts; i++)
        {
            _viewModel.Login = "petro";
            _viewModel.Password = "wrong";
            await _viewModel.SignInCommand.ExecuteAsync(null);
        }

        _viewModel.Login = "petro";
        _viewModel.Password = Password;
        await _viewModel.SignInCommand.ExecuteAsync(null);

        Assert.Contains("Too many failed attempts", _viewModel.ErrorMessage);
        Assert.Null(_store.CurrentUser.CurrentUser);
    }

    [Fact]
    public async Task SignIn_SecretStoreFails_StillSignsInForThisRun_AndLogsAWarning()
    {
        _store.Secrets.Failing = true;
        _viewModel.Login = "petro";
        _viewModel.Password = Password;

        await _viewModel.SignInCommand.ExecuteAsync(null);

        Assert.Same(_storekeeper, _store.CurrentUser.CurrentUser);
        Assert.Contains(_store.Logs, l => l.Level == LogLevel.Warning && l.Category == nameof(LoginViewModel));
    }

    [Fact]
    public async Task Logs_NeverContainThePassword()
    {
        _viewModel.Login = "petro";
        _viewModel.Password = Password;
        await _viewModel.SignInCommand.ExecuteAsync(null);
        _viewModel.Login = "petro";
        _viewModel.Password = "another-secret";
        await _viewModel.SignInCommand.ExecuteAsync(null);

        Assert.All(_store.Logs, l => Assert.DoesNotContain(Password, l.Message));
        Assert.All(_store.Logs, l => Assert.DoesNotContain("another-secret", l.Message));
    }

    // ----- biometrics -----

    private async Task<bool> OfferedAsync()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);
        return _viewModel.IsBiometricAvailable;
    }

    [Fact]
    public async Task BiometricButton_IsOffered_OnlyWithAValidSession_TheSettingOn_AndHardware()
    {
        await _store.Session.SaveAsync(_storekeeper);
        _biometricSettings.IsEnabled = true;

        Assert.True(await OfferedAsync());
        Assert.True(_viewModel.BiometricSignInCommand.CanExecute(null));
    }

    [Fact]
    public async Task BiometricButton_IsHidden_WhenTheSettingIsOff()
    {
        await _store.Session.SaveAsync(_storekeeper);

        Assert.False(await OfferedAsync());
    }

    [Fact]
    public async Task BiometricButton_IsHidden_WithoutASession()
    {
        _biometricSettings.IsEnabled = true;

        Assert.False(await OfferedAsync());
    }

    [Fact]
    public async Task BiometricButton_IsHidden_WhenTheTokenHasExpired()
    {
        await _store.Session.SaveAsync(_storekeeper);
        _biometricSettings.IsEnabled = true;
        _store.Clock.UtcNow += HmacTokenService.Lifetime + TimeSpan.FromDays(1);

        Assert.False(await OfferedAsync());
    }

    [Fact]
    public async Task BiometricButton_IsHidden_WithoutHardware()
    {
        await _store.Session.SaveAsync(_storekeeper);
        _biometricSettings.IsEnabled = true;
        _biometrics.Available = false;

        Assert.False(await OfferedAsync());
        Assert.False(_viewModel.BiometricSignInCommand.CanExecute(null));
    }

    [Fact]
    public async Task BiometricSignIn_Success_RestoresTheSessionFromTheToken()
    {
        await _store.Session.SaveAsync(_storekeeper);
        _biometricSettings.IsEnabled = true;
        await OfferedAsync();

        await _viewModel.BiometricSignInCommand.ExecuteAsync(null);

        Assert.Same(_storekeeper, _store.CurrentUser.CurrentUser);
        Assert.Equal(Routes.Absolute(Routes.Products), Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Single(_biometrics.Reasons);
    }

    [Theory]
    [InlineData(BiometricOutcome.Failed, "failed or cancelled")]
    [InlineData(BiometricOutcome.NotAvailable, "not available")]
    public async Task BiometricSignIn_NotSuccessful_StaysOnThePasswordForm_WithAMessageAndAWarning(
        BiometricOutcome outcome, string expectedText)
    {
        await _store.Session.SaveAsync(_storekeeper);
        _biometricSettings.IsEnabled = true;
        await OfferedAsync();
        _biometrics.Outcome = outcome;

        await _viewModel.BiometricSignInCommand.ExecuteAsync(null);

        Assert.Null(_store.CurrentUser.CurrentUser);
        Assert.Empty(_store.Navigation.RequestedRoutes);
        Assert.Contains(expectedText, _viewModel.ErrorMessage);
        Assert.Contains(_store.Logs, l => l.Level == LogLevel.Warning && l.Message.Contains(outcome.ToString()));
    }

    [Fact]
    public async Task BiometricSignIn_Success_ButTheTokenWasTamperedWith_DoesNotSignIn()
    {
        await _store.Session.SaveAsync(_storekeeper);
        _biometricSettings.IsEnabled = true;
        await OfferedAsync();
        _store.Secrets.Values[SessionStore.TokenKey] = _store.Secrets.Values[SessionStore.TokenKey][..^2] + "AA";

        await _viewModel.BiometricSignInCommand.ExecuteAsync(null);

        Assert.Null(_store.CurrentUser.CurrentUser);
        Assert.Empty(_store.Navigation.RequestedRoutes);
        Assert.False(_viewModel.IsBiometricAvailable);
        Assert.Contains("password", _viewModel.ErrorMessage);
    }

    [Fact]
    public async Task BiometricSignIn_Success_ButTheTokenExpired_DoesNotSignIn()
    {
        await _store.Session.SaveAsync(_storekeeper);
        _biometricSettings.IsEnabled = true;
        await OfferedAsync();
        _store.Clock.UtcNow += HmacTokenService.Lifetime + TimeSpan.FromDays(1);

        await _viewModel.BiometricSignInCommand.ExecuteAsync(null);

        Assert.Null(_store.CurrentUser.CurrentUser);
    }
}
