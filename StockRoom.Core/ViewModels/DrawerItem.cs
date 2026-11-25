using CommunityToolkit.Mvvm.ComponentModel;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// One row of the drawer; the highlight follows the page that is open
public sealed partial class DrawerItem(MenuEntry entry) : ObservableObject
{
    public string Route { get; } = entry.Route;

    // Resource key of the icon
    public string Glyph { get; } = entry.Glyph;

    public MenuEntryKind Kind { get; } = entry.Kind;

    public string Title { get; } = entry.Title;

    public string AutomationId { get; } = $"MenuItem_{char.ToUpperInvariant(entry.Route[0])}{entry.Route[1..]}";

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
