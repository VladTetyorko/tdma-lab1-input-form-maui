using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views.Components;

public partial class DrawerContent : ContentView
{
    public DrawerContent(DrawerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
