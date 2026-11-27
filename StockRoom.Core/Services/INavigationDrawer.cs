namespace Mobile_apps_L1_Model_view.Services;

// The menu that slides in from the side, so ViewModels can close it without knowing Shell
public interface INavigationDrawer
{
    bool IsOpen { get; }

    void Open();

    void Close();
}
