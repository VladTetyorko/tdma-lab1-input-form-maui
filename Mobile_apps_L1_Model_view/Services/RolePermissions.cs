using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// The role table from docs/domain/domain-model.md, in one place
public static class RolePermissions
{
    public static bool CanManageProducts(Role role) => role == Role.Manager;

    public static bool CanRecord(Role role, OperationDirection direction) =>
        direction == OperationDirection.In || role != Role.Driver;
}
