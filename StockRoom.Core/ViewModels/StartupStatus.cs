namespace Mobile_apps_L1_Model_view.ViewModels;

// What the startup page shows: a progress message, or the reason the app could not start
public sealed record StartupStatus(string Message, bool IsBusy)
{
    public static StartupStatus Opening { get; } = new("Opening the database...", true);

    public static StartupStatus Failed(string reason) => new($"StockRoom could not start: {reason}", false);
}
