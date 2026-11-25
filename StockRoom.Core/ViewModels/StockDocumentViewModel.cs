using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// Receive goods (In) or issue goods (Out): scan or type barcodes, adjust quantities, commit the whole batch.
// The line rules live in StockDocumentDraft and the recording in IStockDocumentService; this class only orchestrates
public sealed partial class StockDocumentViewModel(
    IStockDocumentDraftStore drafts,
    IStockDocumentService documents,
    IProductCatalog catalog,
    ICurrentUserService currentUser,
    IBarcodeScanner scanner,
    IScanFeedback feedback,
    IToastService toast,
    IAlertService alerts,
    IPermissionGuard guard,
    INavigationService navigation,
    StockChanges changes,
    IClock clock,
    ILogger<StockDocumentViewModel> logger) : ObservableObject, IPageLifecycleAware
{
    private readonly ScanDebouncer _debouncer = new(clock);
    private readonly Dictionary<string, ProductSummary?> _products = [];
    private StockDocumentDraft _draft = new(OperationDirection.In);
    private (DraftLine Line, int Index)? _removed;
    private bool _loading;
    private bool _createdProduct;

    // Set once by the page, before it appears
    public OperationDirection Direction { get; set; }

    public bool IsIssue => Direction == OperationDirection.Out;

    public string Title => IsIssue ? "Issue goods" : "Receive goods";

    public string CounterpartyLabel => IsIssue ? "Recipient (optional)" : "Supplier (optional)";

    public bool IsCameraSupported => scanner.IsCameraSupported;

    public ObservableCollection<StockDocumentLineItem> Lines { get; } = [];

    [ObservableProperty]
    public partial string ScanText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Counterparty { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Note { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    // "Draft · 5 lines · 37 units"
    [ObservableProperty]
    public partial string SummaryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CommitText { get; set; } = string.Empty;

    // Not empty while a removed line can be brought back
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUndo))]
    public partial string UndoText { get; set; } = string.Empty;

    public bool CanUndo => UndoText.Length > 0;

    public bool IsEmpty => Lines.Count == 0;

    [ObservableProperty]
    public partial bool IsCameraActive { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CommitCommand))]
    public partial bool IsBusy { get; set; }

    public bool CanCreateProducts =>
        currentUser.CurrentUser is { } user && RolePermissions.CanManageProducts(user.Role);

    private bool CanCommit => !IsBusy && Lines.Count > 0 && Lines.All(line => !line.IsInvalid);

    public void OnAppearing() => AppearCommand.Execute(null);

    public void OnDisappearing() => IsCameraActive = false;

    // Products may have been added or their stock changed elsewhere, and another user may have signed in:
    // the draft is read again every time
    [RelayCommand]
    private async Task AppearAsync()
    {
        await LoadAsync();
        if (scanner.IsCameraSupported)
        {
            IsCameraActive = await scanner.RequestCameraAccessAsync();
            if (!IsCameraActive)
            {
                StatusMessage = "Camera access was denied. Type the barcode instead.";
                logger.LogWarning("Camera access was denied, barcodes must be typed");
            }
        }
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

        StatusMessage = string.Empty;
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
        if (await AddAsync(ScanText, debounce: false))
        {
            ScanText = string.Empty;
        }
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
            StatusMessage = "Enter a whole number of units.";
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

    [RelayCommand]
    private async Task DiscardAsync()
    {
        if (_draft.IsEmpty
            || !await alerts.ConfirmAsync("Discard draft", "Remove all lines of this draft?", "Discard", "Keep"))
        {
            return;
        }

        logger.LogInformation("{Direction} draft with {LineCount} lines discarded", Direction, _draft.Lines.Count);
        ClearDraft();
        StatusMessage = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanCommit))]
    private async Task CommitAsync()
    {
        IsBusy = true;
        try
        {
            var units = _draft.TotalUnits;
            var question = $"{(IsIssue ? "Issue" : "Receive")} {units} units of {_draft.Lines.Count} products?";
            if (!await alerts.ConfirmAsync(Title, question, IsIssue ? "Issue" : "Receive", "Cancel"))
            {
                return;
            }

            var result = await documents.CommitAsync(_draft);
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
            StatusMessage = $"Saved {result.Value!.Number}";
            feedback.Success();
            await toast.ShowAsync(StatusMessage);
        }
        finally
        {
            IsBusy = false;
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
        _removed = null;
        await ResolveAsync([barcode]);
        _draft.Add(barcode);
        Refresh();
        if (_products[barcode] is { } product)
        {
            StatusMessage = $"{product.Name} +1";
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
        SummaryText = Lines.Count == 0
            ? "Draft is empty. Scan a barcode or type it above."
            : $"Draft · {Lines.Count} {(Lines.Count == 1 ? "line" : "lines")} · {units} units";
        CommitText = $"{(IsIssue ? "Issue" : "Receive")} {units} units";
        OnPropertyChanged(nameof(IsEmpty));
        CommitCommand.NotifyCanExecuteChanged();
    }
}
