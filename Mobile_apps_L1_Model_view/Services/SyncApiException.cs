namespace Mobile_apps_L1_Model_view.Services;

// The server could not be reached or refused the request; the item stays queued and is retried later
public sealed class SyncApiException(string message, Exception? innerException = null)
    : Exception(message, innerException);
