using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views.Components;

// View side of BottomNavViewModel (one for the app, so every root page shows the same navigation). A root page is
// <Grid RowDefinitions="Auto,*,Auto" ColumnDefinitions="Auto,*"> with the app bar and the content in column 1.
// Under the breakpoint this is a bar in row 2, column 1; from it a rail in column 0 over all rows. The mode follows the
// width of the PARENT (not of this view, which changes with the mode), so switching cannot oscillate
public partial class BottomNav : ContentView
{
    private const string BreakpointKey = "NavRailBreakpoint";
    private const double DefaultBreakpoint = 600;

    private VisualElement? _watched;
    private bool? _rail;

    public BottomNav()
    {
        InitializeComponent();
        BindingContext = IPlatformApplication.Current?.Services.GetService<BottomNavViewModel>();
        Show(rail: false);
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        if (_watched is not null)
        {
            _watched.SizeChanged -= OnParentSizeChanged;
            _watched = null;
        }

        if (Parent is VisualElement parent)
        {
            _watched = parent;
            parent.SizeChanged += OnParentSizeChanged;
            Apply(parent.Width);
        }
    }

    private void OnParentSizeChanged(object? sender, EventArgs e) => Apply(((VisualElement)sender!).Width);

    private void Apply(double parentWidth)
    {
        if (parentWidth <= 0)
        {
            return;
        }

        Show(parentWidth >= Breakpoint());
    }

    private static double Breakpoint() =>
        Application.Current?.Resources.TryGetValue(BreakpointKey, out var value) == true && value is double width
            ? width
            : DefaultBreakpoint;

    private void Show(bool rail)
    {
        if (_rail == rail)
        {
            return;
        }

        _rail = rail;
        Content = rail ? new NavRail() : new BottomNavBar();
        if (rail)
        {
            Grid.SetRow(this, 0);
            Grid.SetRowSpan(this, 3);
            Grid.SetColumn(this, 0);
        }
        else
        {
            Grid.SetRow(this, 2);
            Grid.SetRowSpan(this, 1);
            Grid.SetColumn(this, 1);
        }
    }
}
