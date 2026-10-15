using SQLite;

namespace Mobile_apps_L1_Model_view.Data.Rows;

[Table("SyncQueue")]
public sealed class SyncQueueRow : Row
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
}
