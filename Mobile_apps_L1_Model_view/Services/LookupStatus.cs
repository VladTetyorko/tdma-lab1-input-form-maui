namespace Mobile_apps_L1_Model_view.Services;

public enum LookupStatus
{
    Found,
    NotFound,

    // No network, a timeout or a connection error
    Offline,

    // The service answered, but not with usable data (HTTP error, rate limit, malformed JSON)
    Failed
}
