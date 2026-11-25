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

    // Correcting a quantity records an operation, so it follows the same rule
    public static bool CanCorrect(Role role, OperationDirection direction) => CanRecord(role, direction);

    // entities.md: the Driver may view only the operations he recorded
    public static bool CanViewAllOperations(Role role) => role != Role.Driver;

    // Counting the shelves and posting the differences
    public static bool CanCountStock(Role role) => role == Role.Manager;
}
