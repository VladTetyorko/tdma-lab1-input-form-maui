using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class AuthServiceTests
{
    private const string Password = "Correct#Horse1";

    private readonly Pbkdf2PasswordHasher _hasher = new(iterations: 10);
    private readonly InMemoryUserRepository _users = new();
    private readonly FakeClock _clock = new();
    private readonly List<LogRecord> _logs = [];
    private readonly AuthService _auth;
    private User _user = null!;

    public AuthServiceTests()
    {
        _auth = new AuthService(_users, _hasher, _clock, new CapturingLogger<AuthService>(_logs));
        _user = new User { FullName = "Olena Kovalenko", Role = Role.Manager, Login = "olena", PasswordHash = _hasher.Hash(Password) };
        _users.AddAsync(_user).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Login_CorrectCredentials_ReturnsTheUser_AndLogsInformationWithoutSecrets()
    {
        var result = await _auth.LoginAsync("olena", Password);

        Assert.True(result.IsSuccess);
        Assert.Same(_user, result.Value);
        var log = Assert.Single(_logs);
        Assert.Equal(LogLevel.Information, log.Level);
        Assert.DoesNotContain(Password, log.Message);
        Assert.DoesNotContain("pbkdf2", log.Message);
    }

    [Theory]
    [InlineData("OLENA")]
    [InlineData("  olena  ")]
    public async Task Login_IgnoresCaseAndSurroundingSpacesInTheLogin(string login)
    {
        Assert.True((await _auth.LoginAsync(login, Password)).IsSuccess);
    }

    [Fact]
    public async Task Login_PasswordIsCaseSensitive()
    {
        Assert.False((await _auth.LoginAsync("olena", Password.ToUpperInvariant())).IsSuccess);
    }

    [Fact]
    public async Task Login_UnknownLoginAndWrongPassword_GiveTheSameMessage()
    {
        var unknown = await _auth.LoginAsync("nobody", Password);
        var wrong = await _auth.LoginAsync("olena", "wrong");

        Assert.False(unknown.IsSuccess);
        Assert.False(wrong.IsSuccess);
        Assert.Equal(unknown.Error, wrong.Error);
        Assert.Equal(AuthService.InvalidCredentialsMessage, wrong.Error);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("olena", "")]
    [InlineData("", Password)]
    public async Task Login_EmptyInput_Fails(string login, string password)
    {
        Assert.False((await _auth.LoginAsync(login, password)).IsSuccess);
    }

    [Fact]
    public async Task Login_Failure_LogsAWarningWithoutThePasswordOrTheLoginText()
    {
        await _auth.LoginAsync("olena", "wrong-password-123");
        await _auth.LoginAsync("secret-typed-by-mistake", "x");

        Assert.Equal(2, _logs.Count);
        Assert.All(_logs, l => Assert.Equal(LogLevel.Warning, l.Level));
        Assert.All(_logs, l => Assert.DoesNotContain("wrong-password-123", l.Message));
        Assert.All(_logs, l => Assert.DoesNotContain("secret-typed-by-mistake", l.Message));
        Assert.DoesNotContain(_logs, l => l.Message.Contains("pbkdf2"));
    }

    [Fact]
    public async Task Login_FiveFailures_LockTheLogin_EvenForTheCorrectPassword()
    {
        for (var i = 0; i < AuthService.MaxFailedAttempts; i++)
        {
            Assert.Equal(AuthService.InvalidCredentialsMessage, (await _auth.LoginAsync("olena", "wrong")).Error);
        }

        var locked = await _auth.LoginAsync("olena", Password);

        Assert.False(locked.IsSuccess);
        Assert.Contains("Too many failed attempts", locked.Error);
        Assert.Contains("30 seconds", locked.Error);
        Assert.Contains(_logs, l => l.Level == LogLevel.Warning && l.Message.Contains("locked"));
    }

    [Fact]
    public async Task Login_FourFailures_DoNotLock()
    {
        for (var i = 0; i < AuthService.MaxFailedAttempts - 1; i++)
        {
            await _auth.LoginAsync("olena", "wrong");
        }

        Assert.True((await _auth.LoginAsync("olena", Password)).IsSuccess);
    }

    [Fact]
    public async Task Login_AfterTheLockoutExpires_WorksAgain()
    {
        for (var i = 0; i < AuthService.MaxFailedAttempts; i++)
        {
            await _auth.LoginAsync("olena", "wrong");
        }

        _clock.UtcNow += AuthService.LockoutDuration + TimeSpan.FromSeconds(1);

        Assert.True((await _auth.LoginAsync("olena", Password)).IsSuccess);
    }

    [Fact]
    public async Task Login_Lockout_IsPerLogin()
    {
        var other = new User { FullName = "Petro", Role = Role.Storekeeper, Login = "petro", PasswordHash = _hasher.Hash(Password) };
        await _users.AddAsync(other);
        for (var i = 0; i < AuthService.MaxFailedAttempts; i++)
        {
            await _auth.LoginAsync("olena", "wrong");
        }

        Assert.True((await _auth.LoginAsync("petro", Password)).IsSuccess);
    }

    [Fact]
    public async Task Login_UnknownLogin_IsLockedToo_SoLockoutDoesNotRevealWhichLoginsExist()
    {
        for (var i = 0; i < AuthService.MaxFailedAttempts; i++)
        {
            await _auth.LoginAsync("nobody", "x");
        }

        Assert.Contains("Too many failed attempts", (await _auth.LoginAsync("nobody", "x")).Error);
    }

    [Fact]
    public async Task Login_Success_ResetsTheFailureCount()
    {
        for (var i = 0; i < AuthService.MaxFailedAttempts - 1; i++)
        {
            await _auth.LoginAsync("olena", "wrong");
        }

        await _auth.LoginAsync("olena", Password);
        for (var i = 0; i < AuthService.MaxFailedAttempts - 1; i++)
        {
            await _auth.LoginAsync("olena", "wrong");
        }

        Assert.True((await _auth.LoginAsync("olena", Password)).IsSuccess);
    }
}
