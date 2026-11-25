using System.Reflection;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.Tests.Services;

// The role x action table of docs/steps/09-final.md section 4, written as data. Changing RolePermissions
// without changing this table (and the document) fails the build, so the matrix cannot go stale.
public class RolePermissionsMatrixTests
{
    // Action -> the roles that may do it (Storekeeper, Driver, Manager)
    public static TheoryData<string, Role, bool> Matrix => Build(
        ("CanManageProducts", false, false, true),
        ("CanManagePhotos", false, false, true),
        ("CanViewLogs", false, false, true),
        ("CanUseDiagnostics", false, false, true),
        ("SeesEveryOperation", true, false, true),
        ("RecordIn", true, true, true),
        ("RecordOut", true, false, true));

    [Theory]
    [MemberData(nameof(Matrix))]
    public void RolePermissions_MatchTheDocumentedMatrix(string action, Role role, bool expected) =>
        Assert.Equal(expected, Evaluate(action, role));

    [Fact]
    public void EveryPermissionMethod_IsInTheMatrix()
    {
        var methods = typeof(RolePermissions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Select(m => m.Name)
            .ToHashSet();
        var documented = Matrix.Select(row => (string)row[0]).ToHashSet();

        // CanRecord has a direction parameter, so the matrix has one row per direction
        documented.Remove("RecordIn");
        documented.Remove("RecordOut");
        documented.Add("CanRecord");

        Assert.Equal(documented.Order(), methods.Order());
    }

    [Fact]
    public void EveryRole_IsInTheMatrix() =>
        Assert.Equal(Enum.GetValues<Role>().Length, Matrix.Select(row => (Role)row[1]).Distinct().Count());

    private static bool Evaluate(string action, Role role) => action switch
    {
        "CanManageProducts" => RolePermissions.CanManageProducts(role),
        "CanManagePhotos" => RolePermissions.CanManagePhotos(role),
        "CanViewLogs" => RolePermissions.CanViewLogs(role),
        "CanUseDiagnostics" => RolePermissions.CanUseDiagnostics(role),
        "SeesEveryOperation" => RolePermissions.SeesEveryOperation(role),
        "RecordIn" => RolePermissions.CanRecord(role, OperationDirection.In),
        "RecordOut" => RolePermissions.CanRecord(role, OperationDirection.Out),
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Not in the matrix")
    };

    private static TheoryData<string, Role, bool> Build(params (string Action, bool Storekeeper, bool Driver, bool Manager)[] rows)
    {
        var data = new TheoryData<string, Role, bool>();
        foreach (var (action, storekeeper, driver, manager) in rows)
        {
            data.Add(action, Role.Storekeeper, storekeeper);
            data.Add(action, Role.Driver, driver);
            data.Add(action, Role.Manager, manager);
        }

        return data;
    }
}
