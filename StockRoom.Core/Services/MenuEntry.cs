namespace Mobile_apps_L1_Model_view.Services;

// One entry of the drawer. Glyph is the resource key of its icon (Resources/Styles/Icons.xaml)
public sealed record MenuEntry(string Route, string Title, MenuEntryKind Kind, string Glyph);
