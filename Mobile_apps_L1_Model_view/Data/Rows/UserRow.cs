using System.Text.Json.Serialization;
using SQLite;

namespace Mobile_apps_L1_Model_view.Data.Rows;

[Table("Users")]
public sealed class UserRow : Row
{
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    // Null only for a row written before migration 0003 (the columns could not be NOT NULL)
    public string? Login { get; set; }

    // Local only: JsonIgnore keeps the hash out of the SyncQueue payload that is sent to the server
    [JsonIgnore]
    public string? PasswordHash { get; set; }
}
