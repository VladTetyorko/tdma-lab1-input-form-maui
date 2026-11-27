using SQLite;

namespace Mobile_apps_L1_Model_view.Data.Rows;

// What one table row looks like in storage: text for ids, enums and timestamps (see docs/steps/04-sqlite.md).
// Entities stay free of storage attributes; repositories convert between entity and row.
public abstract class Row
{
    // UUID as text ("D" format)
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    // ISO-8601 UTC, e.g. 2026-01-31T10:15:00.0000000Z
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}
