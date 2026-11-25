using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views.Components;

// The page calls Refresh when it appears (lifecycle forwarding only)
public partial class RecentActivityPanel : ContentView
{
    private RecentActivityViewModel? _viewModel;

    public RecentActivityPanel()
    {
        InitializeComponent();
    }

    public void Refresh()
    {
        if (_viewModel is null && Handler?.MauiContext?.Services.GetService<RecentActivityViewModel>() is { } viewModel)
        {
            Card.BindingContext = _viewModel = viewModel;
        }

        _viewModel?.RefreshCommand.Execute(null);
    }
}
