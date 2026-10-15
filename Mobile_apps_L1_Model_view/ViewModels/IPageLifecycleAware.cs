namespace Mobile_apps_L1_Model_view.ViewModels;

// Implemented by a page's ViewModel to react to its page appearing and disappearing.
// PageLifecycleBehavior (attached in the page's XAML) calls it, so code-behind has no lifecycle code.
public interface IPageLifecycleAware
{
    // The page became visible: start the load command, start the camera
    void OnAppearing();

    // The page is hidden (navigation or app in the background): stop timers, camera, unsubscribe
    void OnDisappearing()
    {
    }
}
