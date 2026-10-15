using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views;

public partial class OperationPage : ContentPage
{
    private readonly OperationViewModel _viewModel;

    public OperationPage(OperationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    // Hardware back button: the same command as the on-screen back arrow and Cancel
    protected override bool OnBackButtonPressed()
    {
        _viewModel.GoBackCommand.Execute(null);
        return true;
    }
}
