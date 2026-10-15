using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views;

// Shown while the database is opened and migrated and the saved session is restored, so the UI thread
// never blocks on storage. If that fails it shows the reason instead of a half-working app.
public partial class StartupPage : ContentPage
{
    public StartupPage(StartupStatus status)
    {
        InitializeComponent();
        BindingContext = status;
    }
}
