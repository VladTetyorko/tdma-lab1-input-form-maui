using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Data;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// The last few operations, read only. A plain list, not a dashboard. Every role sees all of them, like the product history
public sealed partial class RecentActivityViewModel(
    IOperationRepository operations,
    IProductRepository products,
    IUserRepository users,
    IStockDocumentRepository documents,
    ICurrentUserService currentUser,
    IClock clock,
    ILogger<RecentActivityViewModel> logger) : ObservableObject
{
    public const int Count = 5;

    public ObservableCollection<RecentActivityItem> Items { get; } = [];

    [ObservableProperty]
    public partial bool HasItems { get; set; }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            var items = await ReadAsync();
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }

            HasItems = Items.Count > 0;
        }
        catch (Exception ex)
        {
            // An extra panel must never take the page down
            logger.LogWarning(ex, "Recent activity could not be read");
        }
    }

    private async Task<List<RecentActivityItem>> ReadAsync()
    {
        if (currentUser.CurrentUser is null)
        {
            return [];
        }

        var recent = await operations.GetRecentAsync(Count);

        var names = (await users.GetAllAsync()).ToDictionary(u => u.Id, u => u.FullName);
        var items = new List<RecentActivityItem>(recent.Count);
        foreach (var operation in recent)
        {
            var product = await products.GetByIdAsync(operation.ProductId);
            var document = operation.DocumentId is { } id ? await documents.GetByIdAsync(id) : null;
            var isIn = operation.Direction == OperationDirection.In;
            var caption = $"{names.GetValueOrDefault(operation.CreatedByUserId, "Unknown user")} · {ActivityTime.Format(operation.CreatedAt, clock.UtcNow)}";
            items.Add(new RecentActivityItem(
                $"{(isIn ? "+" : "−")}{operation.Quantity}",
                product?.Name ?? "Unknown product",
                document is null ? caption : $"{caption} · {document.Number}",
                isIn));
        }

        return items;
    }
}
