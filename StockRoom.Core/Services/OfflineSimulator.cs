namespace Mobile_apps_L1_Model_view.Services;

public sealed class OfflineSimulator : IOfflineSimulator
{
    private bool _isOffline;

    public bool IsOffline
    {
        get => _isOffline;
        set
        {
            if (_isOffline == value)
            {
                return;
            }

            _isOffline = value;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? Changed;
}
