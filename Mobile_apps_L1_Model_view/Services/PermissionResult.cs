namespace Mobile_apps_L1_Model_view.Services;

public enum PermissionResult
{
    Granted,

    // Asked and refused (or not allowed to ask again)
    Denied,

    // The permission cannot exist here: restricted by policy, or the platform has no such permission or hardware
    Unavailable
}
