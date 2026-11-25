namespace Mobile_apps_L1_Model_view.ViewModels;

// One line of an activity list: the signed amount, what it was, and who and when ("Petro · Today 10:42 · R-…")
public sealed record RecentActivityItem(string Amount, string Title, string Caption, bool IsIn)
{
    public string Summary => $"{Amount} {Title}";

    public bool IsOut => !IsIn;
}
