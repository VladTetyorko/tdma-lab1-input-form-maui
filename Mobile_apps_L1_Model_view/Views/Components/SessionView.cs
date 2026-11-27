using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Views.Components;

// A view that shows the app-wide session status. XAML creates it, so it takes the single instance from the container
// once it is attached; its content binds to it without touching the BindingContext of the page
public class SessionView : ContentView
{
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Content is { } content && Handler?.MauiContext?.Services.GetService<SessionStatusViewModel>() is { } status)
        {
            content.BindingContext = status;
        }
    }
}
