namespace Mobile_apps_L1_Model_view.Tests.Fakes;

// OnAppearing starts its load command without awaiting it (like the real page does); with a real database
// the load finishes a moment later, so tests wait for the condition instead of asserting at once
public static class Eventually
{
    public static async Task WaitAsync(Func<bool> condition, int timeoutMilliseconds = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }

        Assert.True(condition(), "The condition was not met in time.");
    }
}
