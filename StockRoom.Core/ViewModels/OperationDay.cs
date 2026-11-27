using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// A group of rows of the operation table. Header is empty for a table without day headers
public sealed record OperationDay(string Header, IReadOnlyList<OperationRowItem> Rows)
{
    public bool HasHeader => Header.Length > 0;

    // Newest first in, one group per local day out
    public static IReadOnlyList<OperationDay> Group(IEnumerable<OperationEntry> entries, DateTime nowUtc, bool showBy) =>
        [.. entries
            .Select(entry => new OperationRowItem(entry, nowUtc, showBy, ShowDay: false))
            .GroupBy(row => OperationRowItem.Day(row.Entry.At, nowUtc))
            .Select(day => new OperationDay(day.Key, [.. day]))];

    // One list without headers, for a few rows on another page
    public static IReadOnlyList<OperationDay> Single(IEnumerable<OperationEntry> entries, DateTime nowUtc, bool showBy)
    {
        List<OperationRowItem> rows = [.. entries.Select(entry => new OperationRowItem(entry, nowUtc, showBy, ShowDay: true))];
        return rows.Count == 0 ? [] : [new OperationDay(string.Empty, rows)];
    }
}
