namespace Mobile_apps_L1_Model_view.Services;

public enum LookupStatus
{
    Found,
    NotFound,

    // No network, a timeout or a connection error
    Offline,

    // A service answered, but not with usable data (HTTP error, rate limit, malformed JSON); for the whole chain:
    // not every database could be asked and none of the others knows the code
    Failed
}
