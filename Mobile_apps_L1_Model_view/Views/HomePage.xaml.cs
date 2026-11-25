using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views;

public partial class HomePage : ContentPage
{
    // From this width the two tiles stand side by side
    private const double WideWidth = 840;

    private bool? _wide;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        Recent.Show(viewModel.Recent);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0 || _wide == width >= WideWidth)
        {
            return;
        }

        _wide = width >= WideWidth;
        Tiles.ColumnDefinitions = _wide.Value
            ? [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)]
            : [new ColumnDefinition(GridLength.Star)];
        Grid.SetRow(IssueTile, _wide.Value ? 0 : 1);
        Grid.SetColumn(IssueTile, _wide.Value ? 1 : 0);
    }
}
