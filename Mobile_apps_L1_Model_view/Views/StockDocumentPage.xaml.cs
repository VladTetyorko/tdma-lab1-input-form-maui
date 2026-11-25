using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views;

// One page for both batch directions, created as ReceivePage or IssuePage
public partial class StockDocumentPage : ContentPage
{
    public StockDocumentPage(StockDocumentViewModel viewModel, OperationDirection direction)
    {
        viewModel.Direction = direction;
        InitializeComponent();
        BindingContext = viewModel;
    }

    // A keyboard scanner types into the box that has the focus: give it back after every Enter
    private void OnScanCompleted(object? sender, EventArgs e) => Dispatcher.Dispatch(() => ScanEntry.Focus());
}
