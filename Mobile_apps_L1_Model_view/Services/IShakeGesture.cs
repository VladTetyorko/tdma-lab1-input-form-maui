namespace Mobile_apps_L1_Model_view.Services;

// "Shake to act" for one page: started in OnAppearing, stopped in OnDisappearing. One instance per page
public interface IShakeGesture
{
    // True between a successful Start and the next Stop
    bool IsActive { get; }

    // Listens for shakes and runs onShake on the UI thread for each one (at most one per debounce window).
    // Does nothing when the user switched shake off, nobody is signed in, or the device has no accelerometer.
    // Calling it again while active changes nothing
    void Start(Func<Task> onShake);

    // Stops listening; safe to call when not started
    void Stop();
}
