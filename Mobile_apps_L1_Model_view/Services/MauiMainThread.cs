namespace Mobile_apps_L1_Model_view.Services;

public sealed class MauiMainThread : IMainThread
{
    public bool IsMainThread => MainThread.IsMainThread;

    public void BeginInvoke(Action action) => MainThread.BeginInvokeOnMainThread(action);

    public Task InvokeAsync(Func<Task> work) => MainThread.InvokeOnMainThreadAsync(work);
}
