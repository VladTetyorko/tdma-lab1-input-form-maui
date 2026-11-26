using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Services.Lookup;
using Mobile_apps_L1_Model_view.ViewModels.Sheets;

namespace Mobile_apps_L1_Model_view.ViewModels;

// Receive goods (In) or issue goods (Out): scan, pick or type products, adjust quantities, commit the whole batch.
// The line rules live in StockDocumentDraft and the recording in IStockDocumentService; this class only orchestrates
public sealed partial class StockDocumentViewModel : ObservableObject, IPageLifecycleAware, INavigationParameterReceiver
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
    private readonly IProductLookup lookup;
    private readonly ScanDebouncer _debouncer;

    // What the open databases said about the unknown codes of the draft; null while still asking
    private readonly Dictionary<string, LookupResult?> _lookups = [];
    private CancellationTokenSource _lookupCancel = new();
    private readonly Dictionary<string, ProductSummary?> _products = [];
    private StockDocumentDraft _draft = new(OperationDirection.In);
    private (DraftLine Line, int Index)? _removed;
    private bool _loading;
    private bool _createdProduct;

    // The barcode whose quantity gets the cursor on the next rebuild of the lines
    private string? _focusBarcode;

    // The unknown barcode a product is being created for
    private string? _creating;

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
        IProductLookup lookup,
        ILogger<StockDocumentViewModel> logger)
    {
        this.lookup = lookup;
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

    // The lookups still running (tests await it; the page never waits)
    public Task Lookups { get; private set; } = Task.CompletedTask;

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

    // Problems only: a successful scan shows in its line, with a sound and a screen-reader announcement
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    public partial string StatusMessage { get; set; } = string.Empty;

    public bool HasStatus => StatusMessage.Length > 0;

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

    // A page that starts a document for one product (the product page) names it, so its quantity gets the cursor
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationKeys.Barcode, out var value) && value is string barcode)
        {
            _focusBarcode = barcode;
        }
    }

    public void OnAppearing() => AppearCommand.Execute(null);

    public void OnDisappearing()
    {
        _left = true;
        scanner.Failed -= OnCameraFailed;
        CameraState = CameraState.Off;
        IsTyping = false;
        QuickSearch.CloseCommand.Execute(null);
        CancelLookups();
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
        QuickSearch.InStockFirst = IsIssue;
        await QuickSearch.LoadCommand.ExecuteAsync(null);

        StatusMessage = string.Empty;
        SuccessText = string.Empty;
        UndoText = string.Empty;
        _removed = null;
        _products.Clear();
        await ResolveAsync(_draft.Lines.Select(line => line.Barcode));
        // Back from creating the product: its line is resolved now and waits for the quantity
        if (_creating is not null && _products.GetValueOrDefault(_creating) is not null)
        {
            _focusBarcode = _creating;
        }

        _creating = null;
        Refresh();
        OnPropertyChanged(nameof(CanCreateProducts));
        foreach (var barcode in Lines.Where(line => line.IsUnknown).Select(line => line.Barcode).ToList())
        {
            StartLookup(barcode);
        }
    }

    // From the camera, which reports a code again and again while it is in view
    [RelayCommand]
    private Task ScanDetectedAsync(string? text) => AddAsync(text, debounce: true);

    // Typed, or sent by a keyboard (USB, Bluetooth) scanner followed by Enter: never ignored
    [RelayCommand]
    private async Task AddTypedAsync()
    {
        var text = QuickSearch.Query;

        // Cleared before the line is added, so the next read of a keyboard scanner starts in an empty box
        if (ScanPayloadParser.Parse(text).IsSuccess)
        {
            QuickSearch.Query = string.Empty;
        }

        await AddAsync(text, debounce: false);
    }

    // A product chosen in the drop-down
    private async Task AddPickedAsync(ProductSummary product)
    {
        _products[product.Barcode] = product;
        await AddAsync(product.Barcode, debounce: false);
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
        // A box that loses the focus because the lines were rebuilt belongs to an old line: it changes nothing
        if (line is null || !Lines.Any(current => ReferenceEquals(current, line)) || line.QuantityText == line.Quantity.ToString())
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

    // Manager only: opens the new-product wizard with the unknown barcode
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
        _creating = line.Barcode;
        await navigation.GoToAsync(Routes.NewProduct,
            LookupParameters.ForNewProduct(line.Barcode, _lookups.GetValueOrDefault(line.Barcode)));
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
            // A number typed without Enter counts too
            if (ApplyTyped())
            {
                Refresh();
                await drafts.SaveAsync(_draft);
                if (Lines.Any(line => line.IsInvalid))
                {
                    StatusMessage = "Fix the red lines first";
                    feedback.Error();
                    return;
                }
            }

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

    // Takes over the numbers that are typed but not yet confirmed; true when a line changed
    private bool ApplyTyped()
    {
        var changed = false;
        foreach (var line in Lines.Where(line => line.TypedQuantity != line.Quantity))
        {
            _draft.SetQuantity(line.Barcode, line.TypedQuantity);
            changed = true;
        }

        return changed;
    }

    // The button and the summary follow what is typed, before it is confirmed
    private void ShowCommitText()
    {
        var units = Lines.Sum(line => line.TypedQuantity);
        CommitText = $"Review {Plural.Units(units)}";
        CommitHint = Lines.Count == 0 ? "Add a product first"
            : Lines.Any(line => line.IsInvalid) ? "Fix the red lines first"
            : string.Empty;
        CommitCommand.NotifyCanExecuteChanged();
        SummaryText = Lines.Count == 0 ? string.Empty : $"Draft · {Plural.Lines(Lines.Count)} · {Plural.Units(units)}";
    }

    private async Task<bool> AddAsync(string? text, bool debounce)
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
        // The number typed for the line before is taken over first, then this one adds 1
        ApplyTyped();
        _draft.Add(barcode);
        _focusBarcode = _products[barcode] is null ? null : barcode;
        Refresh();
        StatusMessage = string.Empty;
        if (_products[barcode] is { } product)
        {
            feedback.Success();
            feedback.Announce($"{product.Name} {Signed(1)}");
        }
        else
        {
            logger.LogWarning("Barcode {Barcode} matched no product", barcode);
            feedback.Error();
            feedback.Announce($"No product has barcode {barcode}.");
            StartLookup(barcode);
        }

        await drafts.SaveAsync(_draft);
        return true;
    }

    // An unknown line says what the item is, whoever scans it. Never awaited: scanning goes on meanwhile.
    // A found item is kept; a miss is asked again when the code is added again or the page is opened again
    private void StartLookup(string barcode)
    {
        if (_lookups.TryGetValue(barcode, out var known) && known is null or { IsFound: true })
        {
            return;
        }

        _lookups[barcode] = null;
        ShowLookup(barcode);
        Lookups = Task.WhenAll(Lookups, LookUpAsync(barcode, _lookupCancel.Token));
    }

    private async Task LookUpAsync(string barcode, CancellationToken cancellationToken)
    {
        try
        {
            var result = await lookup.FindAsync(barcode, cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
            {
                _lookups[barcode] = result;
                ShowLookup(barcode);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The page was left: asked again when it comes back
        }
    }

    // The line is changed in place: rebuilding the lines would take the cursor away
    private void ShowLookup(string barcode)
    {
        foreach (var line in Lines.Where(line => line.Barcode == barcode && line.IsUnknown))
        {
            line.LookupText = LookupTextFor(barcode);
        }
    }

    private string LookupTextFor(string barcode) =>
        !_lookups.TryGetValue(barcode, out var result) ? string.Empty
        : result is null ? LookupMessages.Searching
        : result.IsFound ? $"{LookupMessages.Found(result)}\n{result.Attribution}"
        : LookupMessages.Failure(result.Status, barcode, CanCreateProducts);

    private void CancelLookups()
    {
        _lookupCancel.Cancel();
        _lookupCancel = new CancellationTokenSource();
        Lookups = Task.CompletedTask;
        foreach (var barcode in _lookups.Where(pair => pair.Value is null).Select(pair => pair.Key).ToList())
        {
            _lookups.Remove(barcode);
        }
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
            Lines.Add(new StockDocumentLineItem(line.Barcode, _products.GetValueOrDefault(line.Barcode), line.Quantity, Direction)
            {
                WantsFocus = line.Barcode == _focusBarcode,
                Typed = ShowCommitText,
                LookupText = _products.GetValueOrDefault(line.Barcode) is null ? LookupTextFor(line.Barcode) : string.Empty
            });
        }

        _focusBarcode = null;

        ShowCommitText();
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasLines));
    }
}
