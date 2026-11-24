namespace Mobile_apps_L1_Model_view.Services;

// One place that turns the outcome of a device request into "one user message + one log line" (AGENTS.md 2a)
public interface IDeviceFeedback
{
    // Logs the outcome (Information for a cancel, Warning for denied / not supported / not enabled, Error with the
    // exception for a failure) and returns the text to show the user. Succeeded logs nothing and returns an empty
    // text: the caller logs its own success. error is the exception of a Failed outcome
    string Report(DeviceFeature feature, DeviceStatus status, Exception? error = null);
}
