namespace Mobile_apps_L1_Model_view.Services;

public enum BiometricOutcome
{
    Success,

    // The check ran and did not match, or the user dismissed the prompt (the plugin does not tell the two apart)
    Failed,

    // No hardware, nothing enrolled, or the platform call failed
    NotAvailable
}
