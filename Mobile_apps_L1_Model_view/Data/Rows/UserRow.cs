using SQLite;

namespace Mobile_apps_L1_Model_view.Data.Rows;

[Table("Users")]
public sealed class UserRow : Row
{
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
