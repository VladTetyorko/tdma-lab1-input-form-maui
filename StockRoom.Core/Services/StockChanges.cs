namespace Mobile_apps_L1_Model_view.Services;

// A counter that goes up when products or stock changed outside the page that shows them. A page remembers the number
// it loaded at and reloads when it differs; no events, so nothing has to be unsubscribed
public sealed class StockChanges
{
    public int Version { get; private set; }

    public void Mark() => Version++;
}
