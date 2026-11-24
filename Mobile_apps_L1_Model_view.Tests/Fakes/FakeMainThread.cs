using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// Runs everything at once on the calling thread (a test is its own "UI thread")
public sealed class FakeMainThread : IMainThread
{
    public bool IsMainThread => true;

    public int Invocations { get; private set; }

    public void BeginInvoke(Action action)
    {
        Invocations++;
        action();
    }

    public Task InvokeAsync(Func<Task> work)
    {
        Invocations++;
        return work();
    }
}
