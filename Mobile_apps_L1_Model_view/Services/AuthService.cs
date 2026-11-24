using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class AuthService(
    IUserRepository users,
    IPasswordHasher hasher,
    IClock clock,
    ILogger<AuthService> logger) : IAuthService
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromSeconds(30);

    public const string InvalidCredentialsMessage = "Wrong login or password.";

    private readonly object _gate = new();

    // Failures per login (ignoring case), in memory only: a restart clears them. Unknown logins are counted too,
    // so the lockout does not tell an attacker which logins exist
    private readonly Dictionary<string, Attempts> _attempts = new(StringComparer.OrdinalIgnoreCase);

    // Verified for an unknown login, so that it takes as long as a known one
    private readonly Lazy<string> _dummyHash = new(() => hasher.Hash(Guid.NewGuid().ToString()));

    public async Task<Result<User>> LoginAsync(string login, string password)
    {
        login = (login ?? string.Empty).Trim();
        password ??= string.Empty;

        if (LockedFor(login) is { } remaining)
        {
            var seconds = (int)Math.Ceiling(remaining.TotalSeconds);
            logger.LogWarning("Login refused: this login is locked for another {Seconds} s", seconds);
            return Result<User>.Failure($"Too many failed attempts. Try again in {seconds} seconds.");
        }

        var user = login.Length == 0 ? null : await users.GetByLoginAsync(login);
        var verified = hasher.Verify(password, user?.PasswordHash ?? _dummyHash.Value);
        if (user is null || !verified)
        {
            var failures = RegisterFailure(login);
            // The login text is not logged: a user may have typed a password into the wrong field
            logger.LogWarning("Login failed for {User}, failed attempts in a row: {Failures}",
                user is null ? "an unknown login" : $"user {user.Id}", failures);
            return Result<User>.Failure(InvalidCredentialsMessage);
        }

        lock (_gate)
        {
            _attempts.Remove(login);
        }

        logger.LogInformation("User {UserId} logged in as {Role}", user.Id, user.Role);
        return Result<User>.Success(user);
    }

    private TimeSpan? LockedFor(string login)
    {
        lock (_gate)
        {
            if (_attempts.TryGetValue(login, out var attempts) && attempts.LockedUntil is { } until)
            {
                var remaining = until - clock.UtcNow;
                if (remaining > TimeSpan.Zero)
                {
                    return remaining;
                }

                _attempts.Remove(login);
            }

            return null;
        }
    }

    private int RegisterFailure(string login)
    {
        lock (_gate)
        {
            var failures = (_attempts.GetValueOrDefault(login)?.Failures ?? 0) + 1;
            _attempts[login] = new Attempts(failures,
                failures >= MaxFailedAttempts ? clock.UtcNow + LockoutDuration : null);
            return failures;
        }
    }

    private sealed record Attempts(int Failures, DateTime? LockedUntil);
}
