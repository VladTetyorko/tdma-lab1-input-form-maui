namespace Mobile_apps_L1_Model_view.Services;

// The shake gesture of the accelerometer. Start and Stop are counted: the sensor runs while at least one Start is
// not yet matched by a Stop, so one page can start before the previous page has stopped
public interface IShakeDetector
{
    // False when the device has no accelerometer
    bool IsSupported { get; }

    // True while the sensor is listening
    bool IsRunning { get; }

    // Raised on a sensor thread: marshal to the UI thread (IMainThread) before touching the UI
    event EventHandler? Shaken;

    // False (and nothing happens) when the device cannot detect shakes
    bool Start();

    // Matches one Start; the sensor stops, and the handler is removed from it, when the last Start is matched
    void Stop();
}
