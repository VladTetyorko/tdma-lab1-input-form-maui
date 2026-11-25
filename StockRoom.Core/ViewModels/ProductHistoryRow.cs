using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// One line of a product's history: the same activity row as on Home, and the operation it stands for
public sealed record ProductHistoryRow(RecentActivityItem Activity, OperationHistoryItem Item)
{
    public bool HasLocation => Item.HasLocation;

    public string LocationText => Item.LocationText;

    public static ProductHistoryRow From(OperationHistoryItem item, DateTime nowUtc)
    {
        var isIn = item.Direction == OperationDirection.In;
        var caption = $"{item.UserName} · {ActivityTime.Format(item.At, nowUtc)}";
        return new ProductHistoryRow(
            new RecentActivityItem(
                $"{(isIn ? "+" : "−")}{item.Quantity}",
                isIn ? "Received" : "Issued",
                item.Details.Length == 0 ? caption : $"{caption} · {item.Details}",
                isIn),
            item);
    }
}
