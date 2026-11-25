using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views.Components;

// View side of BottomNavViewModel (one for the app, so every root page shows the same bar)
public partial class BottomNav : ContentView
{
    public BottomNav()
    {
        InitializeComponent();
        BindingContext = IPlatformApplication.Current?.Services.GetService<BottomNavViewModel>();
    }
}
