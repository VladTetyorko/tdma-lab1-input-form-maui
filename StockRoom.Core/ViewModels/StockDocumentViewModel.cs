using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.ViewModels.Sheets;

namespace Mobile_apps_L1_Model_view.ViewModels;

// Receive goods (In) or issue goods (Out): scan, pick or type products, adjust quantities, commit the whole batch.
// The line rules live in StockDocumentDraft and the recording in IStockDocumentService; this class only orchestrates
public sealed partial class StockDocumentViewModel : ObservableObject, IPageLifecycleAware
{
    private const string WholeNumberMessage = "Enter a whole number of units.";

    private readonly IStockDocumentDraftStore drafts;
    private readonly IStockDocumentService documents;
    private readonly IProductCatalog catalog;
    private readonly ICurrentUserService currentUser;
    private readonly IBarcodeScanner scanner;
    private readonly IScanFeedback feedback;
    private readonly IToastService toast;
    private readonly IConfirmSheet confirm;
    private readonly ISheetService sheets;
    private readonly IPermissionGuard guard;
    private readonly INavigationService navigation;
    private readonly StockChanges changes;
    private readonly ILogger<StockDocumentViewModel> logger;
    private readonly ScanDebouncer _debouncer;
    private readonly Dictionary<string, ProductSummary?> _products = [];
    private StockDocumentDraft _draft = new(OperationDirection.In);
    private (DraftLine Line, int Index)? _removed;
    private bool _loading;
    private bool _createdProduct;

    // The page was left (Back), so a pending return must not close a second page
    private bool _left;

    // How long the success message is read before the page returns to where it was opened from
    public TimeSpan ReturnDelay { get; set; } = TimeSpan.FromSeconds(1.5);

    public StockDocumentViewModel(
        IStockDocumentDraftStore drafts,
        IStockDocumentService documents,
        IProductCatalog catalog,
        ICurrentUserService currentUser,
        IBarcodeScanner scanner,
        IScanFeedback feedback,
        IToastService toast,
        IConfirmSheet confirm,
        ISheetService sheets,
        IPermissionGuard guard,
        INavigationService navigation,
        StockChanges changes,
        IClock clock,
        QuickSearchViewModel quickSearch,
        ILogger<StockDocumentViewModel> logger)
    {
        this.drafts = drafts;
        this.documents = documents;
        this.catalog = catalog;
        this.currentUser = currentUser;
        this.scanner = scanner;
        this.feedback = feedback;
        this.toast = toast;
        this.confirm = confirm;
        this.sheets = sheets;
        this.guard = guard;
        this.navigation = navigation;
        this.changes = changes;
        this.logger = logger;
        _debouncer = new ScanDebouncer(clock);
        QuickSearch = quickSearch;
        quickSearch.Picked += async (_, product) => await AddPickedAsync(product);
        quickSearch.SubmittedText += async (_, _) => await AddTypedAsync();
        quickSearch.CreateRequested += (_, _) => _createdProduct = true;
        quickSearch.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(QuickSearchViewModel.IsOpen))
            {
                OnPropertyChanged(nameof(IsCameraActive));
            }
        };
    }

    // The typing field and its drop-down
    public QuickSearchViewModel QuickSearch { get; }

    // Set once by the page, before it appears
    public OperationDirection Direction { get; set; }

    public bool IsIssue => Direction == OperationDirection.Out;

    public string Title => IsIssue ? "Issue goods" : "Receive goods";

    public ObservableCollection<StockDocumentLineItem> Lines { get; } = [];

    // Units a typed or picked product adds; camera reads always add one
    [ObservableProperty]
    public partial string EntryQuantity { get; set; } = "1";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CameraMessage))]
    [NotifyPropertyChangedFor(nameof(CameraActionText))]
    [NotifyPropertyChangedFor(nameof(IsCameraActive))]
    public partial CameraState CameraState { get; set; }

    // Set by a phone page while a typing field has the focus: the camera makes room for it
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCameraActive))]
    public partial bool IsTyping { get; set; }

    public string CameraMessage => CameraMessages.For(CameraState);

    public string CameraActionText => CameraMessages.ActionFor(CameraState);

    // The preview is shown and reads only while nothing is being typed
    public bool IsCameraActive => CameraState == CameraState.Active && !IsTyping && !QuickSearch.IsOpen;

    // Why the main button is disabled, or empty
    [ObservableProperty]
    public partial string CommitHint { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Counterparty { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Note { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    // "Received 12 units · R-20261125-0001" after a commit; the page shows it for a moment
    [ObservableProperty]
    public partial string SuccessText { get; set; } = string.Empty;

    // "Draft · 5 lines · 37 units"; empty for an empty draft, which has its own empty state
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSummary))]
    public partial string SummaryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CommitText { get; set; } = string.Empty;

    // Not empty while a removed line can be brought back
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUndo))]
    public partial string UndoText { get; set; } = string.Empty;

    public bool CanUndo => UndoText.Length > 0;

    public bool HasSummary => SummaryText.Length > 0;

    public bool IsEmpty => Lines.Count == 0;

    public bool HasLines => !IsEmpty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CommitCommand))]
    public partial bool IsBusy { get; set; }

    public bool CanCreateProducts =>
        currentUser.CurrentUser is { } user && RolePermissions.CanManageProducts(user.Role);

    private bool CanCommit => !IsBusy && Lines.Count > 0 && Lines.All(line => !line.IsInvalid);

    public void OnAppearing() => AppearCommand.Execute(null);

    public void OnDisappearing()
    {
        _left = true;
        scanner.Failed -= OnCameraFailed;
        CameraState = CameraState.Off;
        IsTyping = false;
        QuickSearch.CloseCommand.Execute(null);
    }

    // Products may have been added or their stock changed elsewhere, and another user may have signed in:
    // the draft is read again every time
    [RelayCommand]
    private async Task AppearAsync()
    {
        // The menu hides Issue from a Driver, but a restored or typed route can still arrive here
        if (await guard.CheckAsync(role => RolePermissions.CanRecord(role, Direction),
                IsIssue ? "issue goods" : "receive goods") is not null)
        {
            await navigation.GoToAsync(Routes.Home);
            return;
        }

        await LoadAsync();
        await StartCameraAsync();
    }

    [RelayCommand]
    private Task RetryCameraAsync() => StartCameraAsync();

    private async Task StartCameraAsync()
    {
        if (!scanner.IsCameraSupported)
        {
            CameraState = CameraState.Unsupported;
            return;
        }

        if (await scanner.RequestCameraAccessAsync())
        {
            scanner.Failed -= OnCameraFailed;
            scanner.Failed += OnCameraFailed;
            CameraState = CameraState.Active;
        }
        else
        {
            CameraState = CameraState.Denied;
            logger.LogWarning("Camera access was denied, barcodes must be typed");
        }
    }

    private void OnCameraFailed(object? sender, EventArgs e)
    {
        CameraState = CameraState.Failed;
        logger.LogWarning("The camera could not start, barcodes must be typed");
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (_createdProduct)
        {
            _createdProduct = false;
            changes.Mark();
        }

        _loading = true;
        _draft = await drafts.LoadAsync(Direction);
        Counterparty = _draft.Counterparty;
        Note = _draft.Note;
        _loading = false;
        EntryQuantity = "1";
        QuickSearch.InStockFirst = IsIssue;
        await QuickSearch.LoadCommand.ExecuteAsync(null);

        StatusMessage = string.Empty;
        SuccessText = string.Empty;
        UndoText = string.Empty;
        _removed = null;
        _products.Clear();
        await ResolveAsync(_draft.Lines.Select(line => line.Barcode));
        Refresh();
        OnPropertyChanged(nameof(CanCreateProducts));
    }

    // From the camera, which reports a code again and again while it is in view
    [RelayCommand]
    private Task ScanDetectedAsync(string? text) => AddAsync(text, debounce: true);

    // Typed, or sent by a keyboard (USB, Bluetooth) scanner followed by Enter: never ignored
    [RelayCommand]
    private async Task AddTypedAsync()
    {
        if (ReadEntryQuantity() is not { } quantity)
        {
            return;
        }

        var text = QuickSearch.Query;

        // Cleared before the line is added, so the next read of a keyboard scanner starts in an empty box
        if (ScanPayloadParser.Parse(text).IsSuccess)
        {
            QuickSearch.Query = string.Empty;
        }

        if (await AddAsync(text, debounce: false, quantity))
        {
            EntryQuantity = "1";
        }
    }

    // A product chosen in the drop-down
    private async Task AddPickedAsync(ProductSummary product)
    {
        if (ReadEntryQuantity() is not { } quantity)
        {
            return;
        }

        _products[product.Barcode] = product;
        if (await AddAsync(product.Barcode, debounce: false, quantity))
        {
            EntryQuantity = "1";
        }
    }

    private int? ReadEntryQuantity()
    {
        if (int.TryParse(EntryQuantity, out var quantity) && quantity >= 1)
        {
            return quantity;
        }

        StatusMessage = WholeNumberMessage;
        feedback.Error();
        return null;
    }

    [RelayCommand]
    private Task IncrementAsync(StockDocumentLineItem? line) =>
        line is null ? Task.CompletedTask : ChangeAsync(line.Barcode, line.Quantity + 1);

    [RelayCommand]
    private Task DecrementAsync(StockDocumentLineItem? line) =>
        line is null ? Task.CompletedTask
        : line.Quantity <= 1 ? RemoveLineAsync(line)
        : ChangeAsync(line.Barcode, line.Quantity - 1);

    [RelayCommand]
    private async Task SetQuantityAsync(StockDocumentLineItem? line)
    {
        if (line is null || line.QuantityText == line.Quantity.ToString())
        {
            return;
        }

        if (!int.TryParse(line.QuantityText, out var quantity) || quantity < 0)
        {
            StatusMessage = WholeNumberMessage;
            Refresh();
            return;
        }

        if (quantity == 0)
        {
            await RemoveLineAsync(line);
        }
        else
        {
            await ChangeAsync(line.Barcode, quantity);
        }
    }

    [RelayCommand]
    private async Task RemoveLineAsync(StockDocumentLineItem? line)
    {
        if (line is null || _draft.Remove(line.Barcode) is not { } removed)
        {
            return;
        }

        _removed = removed;
        StatusMessage = string.Empty;
        UndoText = $"Undo removing {line.Name}";
        Refresh();
        await drafts.SaveAsync(_draft);
        await toast.ShowAsync($"Removed {line.Name}", "Undo", () => UndoCommand.Execute(null));
    }

    [RelayCommand]
    private async Task UndoAsync()
    {
        if (_removed is not { } removed)
        {
            return;
        }

        _removed = null;
        UndoText = string.Empty;
        _draft.Restore(removed.Line, removed.Index);
        await ResolveAsync([removed.Line.Barcode]);
        Refresh();
        await drafts.SaveAsync(_draft);
    }

    // Manager only: opens the product form with the unknown barcode
    [RelayCommand]
    private async Task CreateProductAsync(StockDocumentLineItem? line)
    {
        if (line is not { IsUnknown: true })
        {
            return;
        }

        if (await guard.CheckAsync(RolePermissions.CanManageProducts, "add products") is { } refusal)
        {
            StatusMessage = refusal;
            return;
        }

        _createdProduct = true;
        await navigation.GoToAsync(Routes.ProductForm,
            new Dictionary<string, object> { [NavigationKeys.Barcode] = line.Barcode });
    }

    // The ⋮ menu of the page
    [RelayCommand]
    private async Task OpenMenuAsync()
    {
        var menu = new ActionSheetViewModel(Title, new SheetAction("DiscardDraftButton", "Discard draft", true));
        await sheets.ShowAsync(menu);
        if (menu.Chosen is not null)
        {
            await DiscardCommand.ExecuteAsync(null);
        }
    }

    [RelayCommand]
    private async Task DiscardAsync()
    {
        if (_draft.IsEmpty
            || !await confirm.AskAsync(new ConfirmRequest("Discard draft", "Remove all lines of this draft?", "Discard", "Keep")
            {
                IsDestructive = true
            }))
        {
            return;
        }

        logger.LogInformation("{Direction} draft with {LineCount} lines discarded", Direction, _draft.Lines.Count);
        ClearDraft();
        StatusMessage = string.Empty;
    }

    // "Review N units": the sheet shows what will move, takes the optional facts and records the document
    [RelayCommand(CanExecute = nameof(CanCommit))]
    private async Task CommitAsync()
    {
        IsBusy = true;
        try
        {
            var units = _draft.TotalUnits;
            Result<StockDocument>? result = null;
            var review = new DocumentReviewViewModel(Direction, [.. Lines],
                await documents.GetRecentCounterpartiesAsync(Direction), Counterparty, Note,
                async (counterparty, note) =>
                {
                    Counterparty = counterparty;
                    Note = note;
                    result = await documents.CommitAsync(_draft);
                });
            await sheets.ShowAsync(review);
            if (result is null)
            {
                // Back: what was typed in the sheet stays with the draft
                Counterparty = review.Counterparty;
                Note = review.Note;
                return;
            }

            if (!result.IsSuccess)
            {
                // The draft stays; stock is read again so the lines show what changed
                feedback.Error();
                _products.Clear();
                await ResolveAsync(_draft.Lines.Select(line => line.Barcode));
                Refresh();
                StatusMessage = result.Error!;
                return;
            }

            // Cleared only after success, so a kill during the commit never loses the lines
            ClearDraft();
            await QuickSearch.LoadCommand.ExecuteAsync(null);
            StatusMessage = string.Empty;
            SuccessText = $"{(IsIssue ? "Issued" : "Received")} {Plural.Units(units)} · {result.Value!.Number}";
            feedback.Success();
            await ReturnToOriginAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    // The draft is saved with every change, so leaving never asks; it only says the lines are kept
    [RelayCommand]
    private async Task GoBackAsync()
    {
        if (!_draft.IsEmpty && _draft.Lines.Count > 0)
        {
            await toast.ShowAsync($"Draft kept · {Plural.Lines(_draft.Lines.Count)}");
        }

        await navigation.GoToAsync(Routes.Back);
    }

    private async Task ReturnToOriginAsync()
    {
        _left = false;
        await Task.Delay(ReturnDelay);
        if (!_left)
        {
            await navigation.GoToAsync(Routes.Back);
        }
    }

    partial void OnCounterpartyChanged(string value) => SaveText(() => _draft.Counterparty = value);

    partial void OnNoteChanged(string value) => SaveText(() => _draft.Note = value);

    private void SaveText(Action apply)
    {
        if (!_loading)
        {
            apply();
            _ = drafts.SaveAsync(_draft);
        }
    }

    // "+12" when receiving, "−12" when issuing
    private string Signed(int quantity) => $"{(IsIssue ? "−" : "+")}{quantity}";

    private async Task<bool> AddAsync(string? text, bool debounce, int quantity = 1)
    {
        var parsed = ScanPayloadParser.Parse(text);
        if (!parsed.IsSuccess)
        {
            StatusMessage = parsed.Error!;
            logger.LogWarning("Scanned text was not a barcode: {Reason}", parsed.Error);
            feedback.Error();
            return false;
        }

        var barcode = parsed.Value!;
        if (debounce && !_debouncer.Accept(barcode))
        {
            return false;
        }

        UndoText = string.Empty;
        SuccessText = string.Empty;
        _removed = null;
        await ResolveAsync([barcode]);
        _draft.Add(barcode, quantity);
        Refresh();
        if (_products[barcode] is { } product)
        {
            StatusMessage = $"{product.Name} {Signed(quantity)}";
            feedback.Success();
        }
        else
        {
            StatusMessage = $"No product has barcode {barcode}.";
            logger.LogWarning("Barcode {Barcode} matched no product", barcode);
            feedback.Error();
        }

        await drafts.SaveAsync(_draft);
        return true;
    }

    private async Task ChangeAsync(string barcode, int quantity)
    {
        _draft.SetQuantity(barcode, quantity);
        StatusMessage = string.Empty;
        Refresh();
        await drafts.SaveAsync(_draft);
    }

    private void ClearDraft()
    {
        _draft.Clear();
        drafts.Clear(Direction);
        _loading = true;
        Counterparty = string.Empty;
        Note = string.Empty;
        _loading = false;
        UndoText = string.Empty;
        _removed = null;
        _products.Clear();
        Refresh();
    }

    private async Task ResolveAsync(IEnumerable<string> barcodes)
    {
        foreach (var barcode in barcodes.ToList())
        {
            if (!_products.ContainsKey(barcode))
            {
                _products[barcode] = await catalog.GetSummaryByBarcodeAsync(barcode);
            }
        }
    }

    private void Refresh()
    {
        Lines.Clear();
        foreach (var line in _draft.Lines)
        {
            Lines.Add(new StockDocumentLineItem(line.Barcode, _products.GetValueOrDefault(line.Barcode), line.Quantity, Direction));
        }

        var units = _draft.TotalUnits;
        SummaryText = Lines.Count == 0 ? string.Empty : $"Draft · {Plural.Lines(Lines.Count)} · {Plural.Units(units)}";
        CommitText = $"Review {Plural.Units(units)}";
        CommitHint = Lines.Count == 0 ? "Add a product first"
            : Lines.Any(line => line.IsInvalid) ? "Fix the red lines first"
            : string.Empty;
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasLines));
        CommitCommand.NotifyCanExecuteChanged();
    }
}
