namespace Mobile_apps_L1_Model_view.Services;

// Runs code on the UI thread (platform abstraction over MainThread), so ViewModels never touch the static
public interface IMainThread
{
    bool IsMainThread { get; }

    // Fire and forget
    void BeginInvoke(Action action);

    Task InvokeAsync(Func<Task> work);
}
