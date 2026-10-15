using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class CurrentUserServiceTests
{
    [Fact]
    public void Initially_NobodyIsSignedIn()
    {
        Assert.Null(new CurrentUserService().CurrentUser);
    }

    [Fact]
    public void SignIn_SetsCurrentUser()
    {
        var service = new CurrentUserService();
        var user = new User { FullName = "Olena Kovalenko", Role = Role.Manager };

        service.SignIn(user);

        Assert.Same(user, service.CurrentUser);
    }

    [Fact]
    public void SignOut_ClearsCurrentUser()
    {
        var service = new CurrentUserService();
        service.SignIn(new User { FullName = "Ivan Shevchenko", Role = Role.Driver });

        service.SignOut();

        Assert.Null(service.CurrentUser);
    }

    [Fact]
    public void SignIn_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new CurrentUserService().SignIn(null!));
    }
}
