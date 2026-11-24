namespace Mobile_apps_L1_Model_view.Services;

// The single source of "now", so tests can control time
public interface IClock
{
    DateTime UtcNow { get; }
}
