using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views;

// Issue goods as its own page type, so a Shell route and the container can create it
public sealed class IssuePage(StockDocumentViewModel viewModel)
    : StockDocumentPage(viewModel, OperationDirection.Out);
