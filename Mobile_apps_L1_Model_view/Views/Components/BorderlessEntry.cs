namespace Mobile_apps_L1_Model_view.Views.Components;

// An Entry without its native frame: OutlinedField draws the outline and the focus ring. Only this subclass is changed,
// every other Entry keeps its native look (see BorderlessEntryMapping)
public class BorderlessEntry : Entry
{
    // A list is rebuilt, a sheet or a page closes while the box has the focus. If its handler is disconnected then,
    // MAUI crashes in the focus callback ("VirtualView cannot be null"), so these handlers are never auto-disconnected
    public BorderlessEntry() => HandlerProperties.SetDisconnectPolicy(this, HandlerDisconnectPolicy.Manual);
}
