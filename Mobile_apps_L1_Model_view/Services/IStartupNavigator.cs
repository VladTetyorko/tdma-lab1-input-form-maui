namespace Mobile_apps_L1_Model_view.Services;

// Runs once the product list is on screen: opens the link the app was started with, or else puts the
// user back on the form they were filling in when the app was killed
public interface IStartupNavigator
{
    Task RunAsync();
}
