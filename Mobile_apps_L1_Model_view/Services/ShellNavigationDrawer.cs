namespace Mobile_apps_L1_Model_view.Services;

// The Shell flyout behind INavigationDrawer
public sealed class ShellNavigationDrawer : INavigationDrawer
{
    public bool IsOpen => Shell.Current?.FlyoutIsPresented ?? false;

    public void Open() => Present(true);

    public void Close() => Present(false);

    private static void Present(bool open)
    {
        if (Shell.Current is not { } shell)
        {
            return;
        }

        if (shell.Dispatcher.IsDispatchRequired)
        {
            shell.Dispatcher.Dispatch(() => shell.FlyoutIsPresented = open);
        }
        else
        {
            shell.FlyoutIsPresented = open;
        }
    }
}
