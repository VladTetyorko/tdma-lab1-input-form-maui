using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// Every operation as a table, grouped by day: All / In / Out, Mine, and optionally one product. What a role may see is
// the feed's rule; the page only hides the "Mine" chip where it would say nothing
public sealed partial class ActivityViewModel(
    IOperationFeed feed,
    ICurrentUserService currentUser,
    OperationViewer viewer,
    IClock clock,
    ILogger<ActivityViewModel> logger) : ObservableObject, INavigationParameterReceiver, IPageLifecycleAware
{
    private ProductSummary? _product;
    private Guid? _loadedFor;

    public ObservableCollection<OperationDay> Days { get; } = [];

    // "All", "In" or "Out"
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAll))]
    [NotifyPropertyChangedFor(nameof(IsIn))]
    [NotifyPropertyChangedFor(nameof(IsOut))]
    public partial string Direction { get; set; } = "All";

    public bool IsAll => Direction == "All";

    public bool IsIn => Direction == "In";

    public bool IsOut => Direction == "Out";

    [ObservableProperty]
    public partial bool MineOnly { get; set; }

    [ObservableProperty]
    public partial bool CanFilterMine { get; set; }

    // "Nutella 400 g ×" while the table shows one product only
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProduct))]
    public partial string ProductText { get; set; } = string.Empty;

    public bool HasProduct => ProductText.Length > 0;

    [ObservableProperty]
    public partial bool ShowBy { get; set; } = true;

    [ObservableProperty]
    public partial string EmptyText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationKeys.SelectedProduct, out var value) && value is ProductSummary product)
        {
            _product = product;
            ProductText = $"{product.Name}  ×";
            Direction = "All";
            MineOnly = false;
        }
    }

    public void OnAppearing() => LoadCommand.Execute(null);

    [RelayCommand]
    private async Task LoadAsync()
    {
        var user = currentUser.CurrentUser;
        // Another user starts with an unfiltered table
        if (_loadedFor is { } previous && previous != user?.Id)
        {
            Direction = "All";
            MineOnly = false;
            _product = null;
            ProductText = string.Empty;
        }

        _loadedFor = user?.Id;
        CanFilterMine = user is not null && RolePermissions.CanViewAllOperations(user.Role);
        ShowBy = CanFilterMine;
        StatusMessage = string.Empty;
        try
        {
            var entries = await feed.GetAsync(new OperationFilter(
                Direction switch { "In" => OperationDirection.In, "Out" => OperationDirection.Out, _ => null },
                MineOnly, _product?.Id));
            Days.Clear();
            foreach (var day in OperationDay.Group(entries, clock.UtcNow, ShowBy))
            {
                Days.Add(day);
            }

            EmptyText = Days.Count > 0 ? string.Empty : "No operations to show.";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "The activity could not be read");
            StatusMessage = "The operations could not be read. Try again.";
        }
    }

    [RelayCommand]
    private Task ShowDirectionAsync(string? direction)
    {
        Direction = direction ?? "All";
        return LoadAsync();
    }

    [RelayCommand]
    private Task ToggleMineAsync()
    {
        MineOnly = !MineOnly;
        return LoadAsync();
    }

    [RelayCommand]
    private Task ClearProductAsync()
    {
        _product = null;
        ProductText = string.Empty;
        return LoadAsync();
    }

    [RelayCommand]
    private async Task OpenAsync(OperationRowItem? row)
    {
        if (row is not null)
        {
            var result = await viewer.OpenAsync(row.Entry);
            if (result.Changed)
            {
                await LoadAsync();
            }

            StatusMessage = result.Message;
        }
    }
}
