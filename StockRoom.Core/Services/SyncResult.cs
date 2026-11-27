namespace Mobile_apps_L1_Model_view.Services;

public sealed record SyncResult(SyncStatus Status, int Pushed, int Remaining, string? Error = null);
