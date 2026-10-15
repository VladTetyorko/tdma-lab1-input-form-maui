using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class RolePermissionsTests
{
    [Theory]
    [InlineData(Role.Manager, true)]
    [InlineData(Role.Storekeeper, false)]
    [InlineData(Role.Driver, false)]
    public void CanManageProducts_OnlyManager(Role role, bool expected)
    {
        Assert.Equal(expected, RolePermissions.CanManageProducts(role));
    }

    [Theory]
    [InlineData(Role.Storekeeper, OperationDirection.In, true)]
    [InlineData(Role.Storekeeper, OperationDirection.Out, true)]
    [InlineData(Role.Manager, OperationDirection.In, true)]
    [InlineData(Role.Manager, OperationDirection.Out, true)]
    [InlineData(Role.Driver, OperationDirection.In, true)]
    [InlineData(Role.Driver, OperationDirection.Out, false)]
    public void CanRecord_DriverRecordsInOnly(Role role, OperationDirection direction, bool expected)
    {
        Assert.Equal(expected, RolePermissions.CanRecord(role, direction));
    }
}
