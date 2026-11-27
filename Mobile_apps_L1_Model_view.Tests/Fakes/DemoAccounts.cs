using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// The seeded accounts (README, "Demo accounts"). End-to-end tests sign in through the real login page ViewModel
public static class DemoAccounts
{
    public static string LoginOf(Role role) => role switch
    {
        Role.Manager => "olena",
        Role.Storekeeper => "petro",
        _ => "ivan"
    };

    public static string PasswordOf(Role role) => role switch
    {
        Role.Manager => "Manager#2026",
        Role.Storekeeper => "Keeper#2026",
        _ => "Driver#2026"
    };

    public static async Task SignInAsync(IServiceProvider provider, Role role)
    {
        var login = provider.GetRequiredService<LoginViewModel>();
        await login.LoadCommand.ExecuteAsync(null);
        login.Login = LoginOf(role);
        login.Password = PasswordOf(role);
        await login.SignInCommand.ExecuteAsync(null);
        Assert.Equal(role, provider.GetRequiredService<ICurrentUserService>().CurrentRole);
    }
}
