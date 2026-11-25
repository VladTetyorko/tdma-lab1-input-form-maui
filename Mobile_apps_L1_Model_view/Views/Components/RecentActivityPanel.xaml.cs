using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views.Components;

// Either the page hands over its ViewModel (Show), or the page calls Refresh when it appears (lifecycle forwarding only)
public partial class RecentActivityPanel : ContentView
{
    private RecentActivityViewModel? _viewModel;

    public RecentActivityPanel()
    {
        InitializeComponent();
    }

    // For a page whose ViewModel owns the list and refreshes it itself
    public void Show(RecentActivityViewModel viewModel) => Card.BindingContext = _viewModel = viewModel;

    public void Refresh()
    {
        if (_viewModel is null && Handler?.MauiContext?.Services.GetService<RecentActivityViewModel>() is { } viewModel)
        {
            Show(viewModel);
        }

        _viewModel?.RefreshCommand.Execute(null);
    }
}
