using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class PermissionGuardTests
{
    private readonly CurrentUserService _currentUser = new();
    private readonly FakeAlertService _alerts = new();
    private readonly List<LogRecord> _logs = [];
    private readonly PermissionGuard _guard;

    public PermissionGuardTests()
    {
        _guard = new PermissionGuard(_currentUser, _alerts, new CapturingLogger<PermissionGuard>(_logs));
    }

    [Fact]
    public async Task Allowed_ReturnsNull_ShowsNothingAndLogsNothing()
    {
        _currentUser.SignIn(new User { FullName = "Olena", Role = Role.Manager });

        Assert.Null(await _guard.CheckAsync(RolePermissions.CanManageProducts, "delete products"));

        Assert.Empty(_alerts.Shown);
        Assert.Empty(_logs);
    }

    [Fact]
    public async Task Denied_ReturnsTheReason_ShowsItInAnAlert_AndLogsAWarning()
    {
        _currentUser.SignIn(new User { FullName = "Ivan", Role = Role.Driver });

        var message = await _guard.CheckAsync(RolePermissions.CanManageProducts, "delete products");

        Assert.Equal("Your role (Driver) cannot delete products.", message);
        Assert.Equal(("Not allowed", message), Assert.Single(_alerts.Shown));
        var log = Assert.Single(_logs);
        Assert.Equal(LogLevel.Warning, log.Level);
        Assert.Contains("Driver", log.Message);
    }

    [Fact]
    public async Task NobodySignedIn_IsDenied()
    {
        var message = await _guard.CheckAsync(RolePermissions.CanViewLogs, "view the logs");

        Assert.Equal("Sign in before you view the logs.", message);
        Assert.Single(_alerts.Shown);
    }
}
