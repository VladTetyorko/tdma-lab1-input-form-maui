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
    [InlineData(Role.Manager, true)]
    [InlineData(Role.Storekeeper, false)]
    [InlineData(Role.Driver, false)]
    public void CanViewLogs_AndCanUseDiagnostics_OnlyManager(Role role, bool expected)
    {
        Assert.Equal(expected, RolePermissions.CanViewLogs(role));
        Assert.Equal(expected, RolePermissions.CanUseDiagnostics(role));
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

    // The whole role table of docs/domain/domain-model.md in one place: a changed rule must change this table
    [Theory]
    [InlineData(Role.Manager, "ManageProducts", true)]
    [InlineData(Role.Manager, "ViewLogs", true)]
    [InlineData(Role.Manager, "UseDiagnostics", true)]
    [InlineData(Role.Manager, "RecordIn", true)]
    [InlineData(Role.Manager, "RecordOut", true)]
    [InlineData(Role.Storekeeper, "ManageProducts", false)]
    [InlineData(Role.Storekeeper, "ViewLogs", false)]
    [InlineData(Role.Storekeeper, "UseDiagnostics", false)]
    [InlineData(Role.Storekeeper, "RecordIn", true)]
    [InlineData(Role.Storekeeper, "RecordOut", true)]
    [InlineData(Role.Driver, "ManageProducts", false)]
    [InlineData(Role.Driver, "ViewLogs", false)]
    [InlineData(Role.Driver, "UseDiagnostics", false)]
    [InlineData(Role.Driver, "RecordIn", true)]
    [InlineData(Role.Driver, "RecordOut", false)]
    [InlineData(Role.Manager, "ManagePhotos", true)]
    [InlineData(Role.Storekeeper, "ManagePhotos", false)]
    [InlineData(Role.Driver, "ManagePhotos", false)]
    public void RoleMatrix(Role role, string action, bool expected)
    {
        var actual = action switch
        {
            "ManageProducts" => RolePermissions.CanManageProducts(role),
            "ViewLogs" => RolePermissions.CanViewLogs(role),
            "UseDiagnostics" => RolePermissions.CanUseDiagnostics(role),
            "RecordIn" => RolePermissions.CanRecord(role, OperationDirection.In),
            "RecordOut" => RolePermissions.CanRecord(role, OperationDirection.Out),
            "ManagePhotos" => RolePermissions.CanManagePhotos(role),
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RoleMatrix_CoversEveryRole()
    {
        // A new role added to the enum must be added to the matrix above
        Assert.Equal(3, Enum.GetValues<Role>().Length);
    }
}
