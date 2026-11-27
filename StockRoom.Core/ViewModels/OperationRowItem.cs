using System.Globalization;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// One row of the operation table, the only way the app shows an operation: signed amount, product, document number,
// who (initials) and when. ShowDay: the row stands without a day header, so an older time says its day
public sealed record OperationRowItem(OperationEntry Entry, DateTime NowUtc, bool ShowBy, bool ShowDay)
{
    public string Amount => $"{(IsIn ? "+" : "−")}{Entry.Quantity}";

    public bool IsIn => Entry.Direction == OperationDirection.In;

    public bool IsOut => !IsIn;

    public string ProductName => Entry.ProductName;

    public string Caption => Entry.DocumentNumber ?? string.Empty;

    public bool HasCaption => Caption.Length > 0;

    public string Initials => string.Concat(Entry.UserName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2).Select(part => char.ToUpperInvariant(part[0])));

    // "10:42"; without a day header an older one is "Yesterday" or "25 Nov"
    public string Time
    {
        get
        {
            var local = Local(Entry.At);
            if (!ShowDay || local.Date == Local(NowUtc).Date)
            {
                return local.ToString("HH:mm", CultureInfo.InvariantCulture);
            }

            return Day(Entry.At, NowUtc);
        }
    }

    // What a screen reader says for the row
    public string Description => $"{Amount} {ProductName}, {Entry.UserName}, {ActivityTime.Format(Entry.At, NowUtc)}";

    public string AutomationId => $"Operation_{Entry.Barcode}";

    // "Today", "Yesterday", "25 Nov", and with the year when it is not this year
    public static string Day(DateTime utc, DateTime nowUtc)
    {
        var day = Local(utc).Date;
        var today = Local(nowUtc).Date;
        return day == today ? "Today"
            : day == today.AddDays(-1) ? "Yesterday"
            : day.ToString(day.Year == today.Year ? "d MMM" : "d MMM yyyy", CultureInfo.InvariantCulture);
    }

    private static DateTime Local(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
}
