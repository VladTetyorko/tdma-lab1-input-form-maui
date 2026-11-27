using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// What a single operation and a batch document check in the same way, with the same words
public static class OperationRules
{
    // Null when the role may record this direction, otherwise the message for the user
    public static string? RoleRefusal(Role role, OperationDirection direction) =>
        RolePermissions.CanRecord(role, direction) ? null : $"Your role ({role}) cannot record {direction} operations.";

    public static bool ExceedsStock(OperationDirection direction, int quantity, int onHand) =>
        direction == OperationDirection.Out && quantity > onHand;
}
