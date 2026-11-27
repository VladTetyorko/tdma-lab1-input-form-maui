using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// The role table from docs/domain/domain-model.md, in one place
public static class RolePermissions
{
    public static bool CanManageProducts(Role role) => role == Role.Manager;

    // Taking, picking and removing a product photo changes the product, so it is the Manager's
    public static bool CanManagePhotos(Role role) => role == Role.Manager;

    // The in-app log viewer and the diagnostics tools in Settings
    public static bool CanViewLogs(Role role) => role == Role.Manager;

    public static bool CanUseDiagnostics(Role role) => role == Role.Manager;

    public static bool CanRecord(Role role, OperationDirection direction) =>
        direction == OperationDirection.In || role != Role.Driver;
}
