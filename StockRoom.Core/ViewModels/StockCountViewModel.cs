using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.ViewModels.Sheets;

namespace Mobile_apps_L1_Model_view.ViewModels;

// Counting the shelves (Manager): scan or pick a product, type what is there, review the differences, post them. The
// count is a local draft, so a restart keeps it
public sealed partial class StockCountViewModel : ObservableObject, IPageLifecycleAware
{
    public const string RecountText = "Changed since you counted: recount";

    private readonly IProductCatalog _catalog;
    private readonly StockCountService _count;
    private readonly IAppStateService _appState;
    private readonly IPermissionGuard _guard;
    private readonly INavigationService _navigation;
    private readonly IBarcodeScanner _scanner;
    private readonly IScanFeedback _feedback;
    private readonly ISheetService _sheets;
    private readonly IConfirmSheet _confirm;
    private readonly IToastService _toast;
    private readonly IClock _clock;
    private readonly ILogger<StockCountViewModel> _logger;
    private readonly ScanDebouncer _debouncer;
    private StockCountDraft _draft;
    private bool _loading;

    public StockCountViewModel(
        IProductCatalog catalog,
        StockCountService count,
        IAppStateService appState,
        IPermissionGuard guard,
        INavigationService navigation,
        IBarcodeScanner scanner,
        IScanFeedback feedback,
        ISheetService sheets,
        IConfirmSheet confirm,
        IToastService toast,
        IClock clock,
        QuickSearchViewModel quickSearch,
        ILogger<StockCountViewModel> logger)
    {
        _catalog = catalog;
        _count = count;
        _appState = appState;
        _guard = guard;
        _navigation = navigation;
        _scanner = scanner;
        _feedback = feedback;
        _sheets = sheets;
        _confirm = confirm;
        _toast = toast;
        _clock = clock;
        _logger = logger;
        _debouncer = new ScanDebouncer(clock);
        _draft = new StockCountDraft(clock.UtcNow, []);
        QuickSearch = quickSearch;
        quickSearch.Picked += (_, product) => Focus(product.Barcode);
        quickSearch.SubmittedText += (_, text) =>
        {
            quickSearch.Query = string.Empty;
            Find(text);
        };
    }

    // The row whose count should get the cursor (the page does it)
    public event EventHandler<CountRow>? FocusRequested;

    public QuickSearchViewModel QuickSearch { get; }

    public ObservableCollection<CountRow> Rows { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CameraMessage))]
    [NotifyPropertyChangedFor(nameof(CameraActionText))]
    [NotifyPropertyChangedFor(nameof(IsCameraActive))]
    public partial CameraState CameraState { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCameraActive))]
    public partial bool IsTyping { get; set; }

    public bool IsCameraActive => CameraState == CameraState.Active && !IsTyping;

    public string CameraMessage => CameraMessages.For(CameraState);

    public string CameraActionText => CameraMessages.ActionFor(CameraState);

    // "7 of 10 counted"
    [ObservableProperty]
    public partial string Progress { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ReviewText { get; set; } = "Review count";

    [ObservableProperty]
    public partial string ReviewHint { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    private bool CanReview => Rows.Any(row => row.Counted is not null);

    public void OnAppearing() => AppearCommand.Execute(null);

    public void OnDisappearing()
    {
        _scanner.Failed -= OnCameraFailed;
        CameraState = CameraState.Off;
        IsTyping = false;
    }

    [RelayCommand]
    private async Task AppearAsync()
    {
        // The drawer shows the entry to the Manager only, but a route can still arrive here
        if (await _guard.CheckAsync(RolePermissions.CanCountStock, "count stock") is not null)
        {
            await _navigation.GoToAsync(Routes.Home);
            return;
        }

        await LoadAsync();
        await QuickSearch.LoadCommand.ExecuteAsync(null);
        await StartCameraAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        _draft = _appState.GetDraft<StockCountDraft>() ?? new StockCountDraft(_clock.UtcNow, []);
        _loading = true;
        foreach (var row in Rows)
        {
            row.PropertyChanged -= OnRowChanged;
        }

        Rows.Clear();
        foreach (var product in (await _catalog.GetSummariesAsync()).OrderBy(product => product.Name))
        {
            var counted = _draft.Counts.GetValueOrDefault(product.Id);
            var row = new CountRow(product, counted?.Counted.ToString() ?? string.Empty);
            if (counted is not null && counted.OnHandWhenCounted != product.Quantity)
            {
                row.Problem = RecountText;
            }

            row.PropertyChanged += OnRowChanged;
            Rows.Add(row);
        }

        _loading = false;
        StatusMessage = string.Empty;
        Refresh();
    }

    [RelayCommand]
    private Task RetryCameraAsync() => StartCameraAsync();

    // A scan finds the product; the number is typed, never added up
    [RelayCommand]
    private void ScanDetected(string? text)
    {
        if (text is not null && _debouncer.Accept(text))
        {
            Find(text);
        }
    }

    // Enter on a count: on to the next product that is not counted yet
    [RelayCommand]
    private void Next(CountRow? row)
    {
        if (row is null)
        {
            return;
        }

        var next = Rows.Skip(Rows.IndexOf(row) + 1).Concat(Rows).FirstOrDefault(other => other.Counted is null);
        if (next is not null)
        {
            FocusRequested?.Invoke(this, next);
        }
    }

    [RelayCommand]
    private async Task OpenMenuAsync()
    {
        var menu = new ActionSheetViewModel("Stock count", new SheetAction("DiscardCountButton", "Discard count", true));
        await _sheets.ShowAsync(menu);
        if (menu.Chosen is null || _draft.Counts.Count == 0
            || !await _confirm.AskAsync(new ConfirmRequest("Discard the count?", "What you counted is not posted.",
                "Discard count", "Keep counting")
            {
                IsDestructive = true
            }))
        {
            return;
        }

        _appState.ClearDraft<StockCountDraft>();
        _logger.LogInformation("Stock count discarded");
        await LoadAsync();
    }

    [RelayCommand(CanExecute = nameof(CanReview))]
    private async Task ReviewAsync()
    {
        var plan = await _count.PlanAsync(_draft);
        Result<IReadOnlyList<Models.StockDocument>>? result = null;
        var sheet = new CountReviewViewModel(plan, _count.Note, async () => result = await _count.PostAsync(_draft));
        await _sheets.ShowAsync(sheet);
        if (result is null)
        {
            return;
        }

        if (!result.IsSuccess)
        {
            _feedback.Error();
            StatusMessage = result.Error!;
            await LoadAsync();
            return;
        }

        _feedback.Success();
        var numbers = string.Join(" · ", result.Value!.Select(document => document.Number));
        await _toast.ShowAsync(numbers.Length == 0 ? "Stock count finished: everything matches" : $"Stock count posted · {numbers}");

        // Rows that must be counted again stay in the draft
        var recount = plan.Recounts.Select(line => line.Product.Id).ToHashSet();
        if (recount.Count == 0)
        {
            _appState.ClearDraft<StockCountDraft>();
            await _navigation.GoToAsync(Routes.Back);
            return;
        }

        _appState.SetDraft(_draft with
        {
            Counts = _draft.Counts.Where(pair => recount.Contains(pair.Key)).ToDictionary()
        });
        await LoadAsync();
    }

    private void Find(string? text)
    {
        var parsed = ScanPayloadParser.Parse(text);
        if (!parsed.IsSuccess)
        {
            StatusMessage = parsed.Error!;
            _feedback.Error();
            return;
        }

        Focus(parsed.Value!);
    }

    private void Focus(string barcode)
    {
        if (Rows.FirstOrDefault(row => row.Barcode == barcode) is not { } row)
        {
            StatusMessage = $"No product has barcode {barcode}.";
            _logger.LogWarning("Barcode {Barcode} matched no product in the count", barcode);
            _feedback.Error();
            return;
        }

        StatusMessage = string.Empty;
        _feedback.Success();
        FocusRequested?.Invoke(this, row);
    }

    private void OnRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_loading || e.PropertyName != nameof(CountRow.CountedText) || sender is not CountRow row)
        {
            return;
        }

        if (row.Counted is { } counted)
        {
            _draft.Counts[row.Product.Id] = new CountedProduct(counted, row.OnHand);
        }
        else
        {
            _draft.Counts.Remove(row.Product.Id);
        }

        row.Problem = string.Empty;
        if (_draft.Counts.Count == 0)
        {
            _appState.ClearDraft<StockCountDraft>();
        }
        else
        {
            _appState.SetDraft(_draft);
        }

        Refresh();
    }

    private void Refresh()
    {
        var counted = Rows.Count(row => row.Counted is not null);
        var differences = Rows.Count(row => row.IsDifferent && row.Problem.Length == 0);
        Progress = $"{counted} of {Rows.Count} counted";
        ReviewText = differences switch
        {
            0 => "Review count",
            1 => "Review 1 difference",
            _ => $"Review {differences} differences"
        };
        ReviewHint = counted == 0 ? "Count a product first" : string.Empty;
        ReviewCommand.NotifyCanExecuteChanged();
    }

    private async Task StartCameraAsync()
    {
        if (!_scanner.IsCameraSupported)
        {
            CameraState = CameraState.Unsupported;
        }
        else if (await _scanner.RequestCameraAccessAsync())
        {
            _scanner.Failed -= OnCameraFailed;
            _scanner.Failed += OnCameraFailed;
            CameraState = CameraState.Active;
        }
        else
        {
            CameraState = CameraState.Denied;
            _logger.LogWarning("Camera access was denied, barcodes must be typed");
        }
    }

    private void OnCameraFailed(object? sender, EventArgs e)
    {
        CameraState = CameraState.Failed;
        _logger.LogWarning("The camera could not start, barcodes must be typed");
    }
}
