using Mobile_apps_L1_Model_view.Services;
using System.Windows.Input;

namespace Mobile_apps_L1_Model_view.ViewModels;

// The left button of the app bar: the menu on a root page, Back everywhere else
public sealed class AppBarViewModel(INavigationDrawer drawer, INavigationService navigation)
{
    // back: the page's own way back (it may have to save or ask first); without one the page is simply closed
    public Task LeftAsync(string? route, ICommand? back)
    {
        if (Routes.IsRoot(route))
        {
            drawer.Open();
        }
        else if (back is not null)
        {
            back.Execute(null);
        }
        else
        {
            return navigation.GoToAsync(Routes.Back);
        }

        return Task.CompletedTask;
    }
}
