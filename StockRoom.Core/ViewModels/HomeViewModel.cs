using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// What needs the user today, as lists to act on and nothing else: unfinished documents, what has run out, and today's
// operations (a Driver: his own deliveries). Receiving and issuing are the pill of the bottom bar
public sealed partial class HomeViewModel(
    ICurrentUserService currentUser,
    IStockDocumentDraftStore drafts,
    IProductCatalog catalog,
    IOperationFeed feed,
    OperationViewer viewer,
    IStartupNavigator startup,
    IShakeGesture shake,
    INavigationService navigation,
    IClock clock,
    StockChanges changes,
    ILogger<HomeViewModel> logger) : ObservableObject, IPageLifecycleAware
{
    public const int OutOfStockShown = 3;
    public const int TodayShown = 5;

    public ObservableCollection<DraftRow> Drafts { get; } = [];

    public ObservableCollection<ProductSummary> OutOfStock { get; } = [];

    public ObservableCollection<OperationDay> Today { get; } = [];

    [ObservableProperty]
    public partial string Greeting { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasDrafts { get; set; }

    [ObservableProperty]
    public partial bool HasOutOfStock { get; set; }

    [ObservableProperty]
    public partial string OutOfStockTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TodayTitle { get; set; } = "Today";

    [ObservableProperty]
    public partial bool ShowBy { get; set; } = true;

    // Shown instead of today's table when it has no rows
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasToday))]
    public partial string TodayEmptyText { get; set; } = string.Empty;

    public bool HasToday => TodayEmptyText.Length == 0;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public void OnAppearing() => AppearCommand.Execute(null);

    public void OnDisappearing() => shake.Stop();

    // Home is the first page after sign-in, so a deep link or an unfinished page of the last run continues from here
    [RelayCommand]
    private async Task AppearAsync()
    {
        shake.Start(() => navigation.GoToAsync(Routes.Search));
        await LoadAsync();
        await startup.RunAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var user = currentUser.CurrentUser;
        StatusMessage = string.Empty;
        if (user is null)
        {
            return;
        }

        Greeting = $"{TimeOfDay()}, {user.FullName.Split(' ')[0]}";
        var seesAll = RolePermissions.CanViewAllOperations(user.Role);
        ShowBy = seesAll;
        TodayTitle = seesAll ? "Today" : "My deliveries today";
        try
        {
            await LoadDraftsAsync(user.Role);
            await LoadOutOfStockAsync(seesAll);
            await LoadTodayAsync(seesAll);
        }
        catch (Exception ex)
        {
            // The lists are a help: the page keeps working without them
            logger.LogWarning(ex, "Home could not be read");
            StatusMessage = "Today's lists could not be read.";
        }
    }

    [RelayCommand]
    private Task ContinueAsync(DraftRow? row) => row is null ? Task.CompletedTask : navigation.GoToAsync(row.Route);

    // The product table with the empty ones first
    [RelayCommand]
    private Task OpenOutOfStockAsync()
    {
        changes.Mark();
        return navigation.GoToAsync(Routes.Products,
            new Dictionary<string, object> { [NavigationKeys.SortByQuantity] = true });
    }

    [RelayCommand]
    private Task OpenActivityAsync() => navigation.GoToAsync(Routes.Activity);

    [RelayCommand]
    private Task OpenProductAsync(ProductSummary? product) =>
        product is null
            ? Task.CompletedTask
            : navigation.GoToAsync(Routes.ProductDetail,
                new Dictionary<string, object> { [NavigationKeys.SelectedProduct] = product });

    [RelayCommand]
    private async Task OpenOperationAsync(OperationRowItem? row)
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

    private async Task LoadDraftsAsync(Role role)
    {
        Drafts.Clear();
        foreach (var direction in new[] { OperationDirection.In, OperationDirection.Out })
        {
            if (!RolePermissions.CanRecord(role, direction) || drafts.LineCount(direction) == 0)
            {
                continue;
            }

            var draft = await drafts.LoadAsync(direction);
            var isIn = direction == OperationDirection.In;
            Drafts.Add(new DraftRow(isIn ? Routes.Receive : Routes.Issue,
                $"{(isIn ? "Receive" : "Issue")} draft · {Plural.Lines(draft.Lines.Count)} · {Plural.Units(draft.TotalUnits)}"));
        }

        HasDrafts = Drafts.Count > 0;
    }

    // Not for a role that only delivers
    private async Task LoadOutOfStockAsync(bool shown)
    {
        List<ProductSummary> empty = shown
            ? [.. (await catalog.GetSummariesAsync()).Where(p => p.Quantity <= 0).OrderBy(p => p.Name)]
            : [];
        OutOfStock.Clear();
        foreach (var product in empty.Take(OutOfStockShown))
        {
            OutOfStock.Add(product);
        }

        HasOutOfStock = empty.Count > 0;
        OutOfStockTitle = $"Out of stock · {empty.Count}";
    }

    private async Task LoadTodayAsync(bool seesAll)
    {
        var midnight = DateTime.SpecifyKind(clock.UtcNow, DateTimeKind.Utc).ToLocalTime().Date;
        var entries = await feed.GetAsync(new OperationFilter(SinceUtc: midnight.ToUniversalTime(), Count: TodayShown));
        Today.Clear();
        foreach (var day in OperationDay.Single(entries, clock.UtcNow, seesAll))
        {
            Today.Add(day);
        }

        TodayEmptyText = Today.Count > 0 ? string.Empty
            : seesAll ? "Nothing recorded today."
            : "Nothing delivered today. Tap + Receive to book a delivery.";
    }

    private string TimeOfDay() => clock.UtcNow.ToLocalTime().Hour switch
    {
        < 12 => "Good morning",
        < 18 => "Good afternoon",
        _ => "Good evening"
    };
}
