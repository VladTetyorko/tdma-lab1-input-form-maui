using Mobile_apps_L1_Model_view.ViewModels.Sheets;

namespace Mobile_apps_L1_Model_view.Views.Sheets;

// View of one SheetViewModel; Closed completes once
public partial class AppSheet : ContentPage
{
    private readonly SheetViewModel _viewModel;
    private readonly TaskCompletionSource _closed = new();

    public AppSheet(SheetViewModel viewModel, View content)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        content.BindingContext = viewModel;
        Body.Content = content;
        PrimaryButton.StyleClass = [viewModel.IsDestructive ? "Danger" : "Primary"];
        SetDynamicResource(BackgroundColorProperty, "Overlay");
        viewModel.Closed += (_, _) => _closed.TrySetResult();
    }

    public Task Closed => _closed.Task;

    protected override bool OnBackButtonPressed()
    {
        _viewModel.Dismiss();
        return true;
    }

    private void OnDismiss(object? sender, EventArgs e) => _viewModel.Dismiss();
}
