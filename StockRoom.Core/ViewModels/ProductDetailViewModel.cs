using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.ViewModels.Sheets;

namespace Mobile_apps_L1_Model_view.ViewModels;

// Opened from the list with the selected ProductSummary in the navigation parameters.
// Characteristic edits that are not saved yet are kept as a draft in IAppStateService, so they
// survive the page reloading when it appears again (after the operation page) and the app being stopped.
public partial class ProductDetailViewModel(
    IProductCatalog catalog,
    ICharacteristicService characteristics,
    ICharacteristicValidator validator,
    ICurrentUserService currentUser,
    IPermissionGuard guard,
    IAppStateService appState,
    INavigationService navigation,
    IProductService productService,
    ProductPhotoEditor photo,
    IOperationFeed feed,
    OperationViewer viewer,
    ISheetService sheets,
    IConfirmSheet confirm,
    ILogger<ProductDetailViewModel> logger,
    IStockDocumentDraftStore drafts,
    IClock clock) : ObservableObject, INavigationParameterReceiver, IPageLifecycleAware
{
    private ProductSummary? _product;

    // Stored value of each characteristic as last loaded: an edit is "unsaved" when it differs
    private Dictionary<Guid, string> _stored = [];

    // True while items are being built or the draft applied (those assignments are not user edits)
    private bool _rebuilding;

    public ObservableCollection<CharacteristicEditItem> Characteristics { get; } = [];

    // The photo of the product; a change is staged and saved with Save (see ProductPhotoEditor)
    public ProductPhotoEditor Photo { get; } = photo;

    public const int HistoryShown = 5;

    // The last operations on this product that the role may see, as an operation table without day headers
    public ObservableCollection<OperationDay> History { get; } = [];

    [ObservableProperty]
    public partial bool HasHistory { get; set; }

    [ObservableProperty]
    public partial bool ShowBy { get; set; } = true;

    // "Weight (kg) 0.14 · Brand Danone"; empty without values
    [ObservableProperty]
    public partial string Summary { get; set; } = string.Empty;

    // "Details" shows every characteristic on its own line
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsListShown))]
    public partial bool IsDetailsOpen { get; set; }

    public bool IsListShown => IsDetailsOpen && !IsEditing;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Barcode { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OnHandText))]
    [NotifyPropertyChangedFor(nameof(IsOutOfStock))]
    [NotifyPropertyChangedFor(nameof(IsInStock))]
    public partial int Quantity { get; set; }

    public string OnHandText => $"{Quantity} on hand";

    public bool IsOutOfStock => Quantity <= 0;

    public bool IsInStock => !IsOutOfStock;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleEditCommand))]
    [NotifyPropertyChangedFor(nameof(CanStartEditing))]
    public partial bool CanEdit { get; set; }

    // The characteristics and the photo are read only until Edit is chosen; the bottom bar then saves
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyPropertyChangedFor(nameof(IsViewing))]
    [NotifyPropertyChangedFor(nameof(CanStartEditing))]
    [NotifyPropertyChangedFor(nameof(IsListShown))]
    public partial bool IsEditing { get; set; }

    public bool IsViewing => !IsEditing;

    public bool CanStartEditing => CanEdit && !IsEditing;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationKeys.SelectedProduct, out var value) && value is ProductSummary product)
        {
            _product = product;
            IsEditing = false;
            IsDetailsOpen = false;
            ShowSummary(product);
        }
    }

    public void OnAppearing() => LoadCommand.Execute(null);

    // Everything else is read from the services every time the page appears,
    // so it also refreshes after the operation page records something
    [RelayCommand]
    private async Task LoadAsync()
    {
        if (_product is null)
        {
            return;
        }

        var details = await catalog.GetDetailsAsync(_product.Id);
        if (details is null)
        {
            StatusMessage = "This product no longer exists.";
            logger.LogWarning("Product {ProductId} no longer exists", _product.Id);
            return;
        }

        var user = currentUser.CurrentUser;
        CanEdit = user is not null && RolePermissions.CanManageProducts(user.Role);
        _product = details.Summary;
        ShowSummary(details.Summary);
        Photo.Load(details.Summary.PhotoPath);

        _rebuilding = true;
        RebuildCharacteristics(details.Characteristics);
        RestoreDraft();
        _rebuilding = false;

        Summary = string.Join(" · ", details.Characteristics
            .Where(c => c.Value.Length > 0)
            .Select(c => $"{c.Name} {c.Value}"));
        ShowBy = user is not null && RolePermissions.CanViewAllOperations(user.Role);
        var recent = await feed.GetAsync(new OperationFilter(ProductId: _product.Id, Count: HistoryShown));
        History.Clear();
        foreach (var day in OperationDay.Single(recent, clock.UtcNow, ShowBy))
        {
            History.Add(day);
        }

        HasHistory = History.Count > 0;
        StatusMessage = string.Empty;
        SaveCommand.NotifyCanExecuteChanged();
        logger.LogDebug("Product {ProductId} details loaded: {Count} operations shown", _product.Id, recent.Count);
    }

    // Into edit mode, or out of it: leaving drops what was not saved
    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void ToggleEdit()
    {
        if (IsEditing)
        {
            Photo.Discard();
            appState.ClearDraft<ProductDetailDraft>();
            _rebuilding = true;
            foreach (var item in Characteristics)
            {
                item.Value = _stored.GetValueOrDefault(item.CharacteristicId, string.Empty);
            }

            _rebuilding = false;
            StatusMessage = string.Empty;
        }

        IsEditing = !IsEditing;
    }

    [RelayCommand]
    private void ToggleDetails() => IsDetailsOpen = !IsDetailsOpen;

    // The ⋮ menu: what the role cannot do is not in it; the commands and services refuse it as well
    [RelayCommand]
    private async Task OpenMenuAsync()
    {
        if (_product is null)
        {
            return;
        }

        List<SheetAction> actions = [];
        if (CanEdit)
        {
            actions.Add(new SheetAction("EditDetailButton", "Edit details"));
            actions.Add(new SheetAction("ChangePhotoButton", "Change photo"));
        }

        actions.Add(new SheetAction("RecordOperationButton", "Record with reason or location"));
        if (CanEdit && _product.CanDelete)
        {
            actions.Add(new SheetAction("DeleteProductButton", "Delete product", true));
        }

        var menu = new ActionSheetViewModel(Name, [.. actions]);
        await sheets.ShowAsync(menu);
        switch (menu.Chosen)
        {
            case "EditDetailButton":
                ToggleEdit();
                break;
            case "ChangePhotoButton":
                await OpenPhotoMenuAsync();
                break;
            case "RecordOperationButton":
                await OpenOperationAsync();
                break;
            case "DeleteProductButton":
                await DeleteAsync();
                break;
        }
    }

    // Take / Pick / Remove; also behind a tap on the photo area
    [RelayCommand]
    private async Task OpenPhotoMenuAsync()
    {
        if (!CanEdit)
        {
            return;
        }

        List<SheetAction> actions = [];
        if (Photo.IsCaptureSupported)
        {
            actions.Add(new SheetAction("TakePhotoButton", "Take photo"));
        }

        actions.Add(new SheetAction("PickPhotoButton", "Pick photo"));
        if (Photo.HasPhoto)
        {
            actions.Add(new SheetAction("RemovePhotoButton", "Remove photo", true));
        }

        var menu = new ActionSheetViewModel("Photo", [.. actions]);
        await sheets.ShowAsync(menu);
        switch (menu.Chosen)
        {
            case "TakePhotoButton":
                await TakePhotoAsync();
                break;
            case "PickPhotoButton":
                await PickPhotoAsync();
                break;
            case "RemovePhotoButton":
                await RemovePhotoAsync();
                break;
        }
    }

    // Only a product without operations can go (the service refuses otherwise and says why)
    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (_product is null)
        {
            return;
        }

        if (await guard.CheckAsync(RolePermissions.CanManageProducts, "delete products") is { } refusal)
        {
            StatusMessage = refusal;
            return;
        }

        if (!await confirm.AskAsync(new ConfirmRequest($"Delete {Name}?", "It has no operations yet.", "Delete product", "Keep")
            {
                IsDestructive = true
            }))
        {
            return;
        }

        var result = await productService.DeleteAsync(_product.Id);
        if (!result.IsSuccess)
        {
            StatusMessage = result.Error!;
            logger.LogWarning("Delete of product {ProductId} was refused", _product.Id);
            return;
        }

        await navigation.GoToAsync(Routes.Back, new Dictionary<string, object> { [NavigationKeys.ProductsChanged] = true });
    }

    // A tap on a row of the history
    [RelayCommand]
    private async Task OpenHistoryRowAsync(OperationRowItem? row)
    {
        if (row is not null)
        {
            StatusMessage = await viewer.OpenAsync(row.Entry);
        }
    }

    // All operations of this product, on the Activity page
    [RelayCommand]
    private Task OpenHistoryAsync() =>
        _product is null
            ? Task.CompletedTask
            : navigation.GoToAsync(Routes.Activity,
                new Dictionary<string, object> { [NavigationKeys.SelectedProduct] = _product });

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (await guard.CheckAsync(RolePermissions.CanManageProducts, "edit characteristic values") is { } refusal)
        {
            StatusMessage = refusal;
            return;
        }

        var values = Characteristics.ToDictionary(c => c.CharacteristicId, c => (string?)c.Value);
        var result = await characteristics.SetValuesAsync(_product!.Id, values);
        if (!result.IsSuccess)
        {
            StatusMessage = result.Error!;
            logger.LogWarning("Characteristic values of product {ProductId} were not saved: {Reason}", _product.Id, result.Error);
            return;
        }

        logger.LogDebug("Characteristic values of product {ProductId} saved, {Changes} changed", _product.Id, result.Value);
        if (Photo.IsChanged)
        {
            var photoResult = await productService.SetPhotoAsync(_product.Id, Photo.Path);
            if (!photoResult.IsSuccess)
            {
                StatusMessage = photoResult.Error!;
                logger.LogWarning("Photo of product {ProductId} was not saved: {Reason}", _product.Id, photoResult.Error);
                return;
            }

            Photo.Commit();
        }

        appState.ClearDraft<ProductDetailDraft>();
        IsEditing = false;
        await ReturnToListAsync();
    }

    [RelayCommand]
    private Task TakePhotoAsync() => ChangePhotoAsync(() => Photo.TakeAsync(Barcode));

    [RelayCommand]
    private Task PickPhotoAsync() => ChangePhotoAsync(() => Photo.PickAsync(Barcode));

    [RelayCommand]
    private Task RemovePhotoAsync() => ChangePhotoAsync(Photo.RemoveAsync);

    // A changed photo is staged, so the page goes into edit mode and offers Save
    private async Task ChangePhotoAsync(Func<Task<string>> change)
    {
        StatusMessage = await change();
        SaveDraft();
        IsEditing |= CanEdit && Photo.IsChanged;
    }

    // Back asks before it throws unsaved edits away
    [RelayCommand]
    private async Task GoBackAsync()
    {
        var unsaved = IsEditing && (Photo.IsChanged
            || Characteristics.Any(c => c.Value != _stored.GetValueOrDefault(c.CharacteristicId, string.Empty)));
        if (unsaved && !await confirm.AskAsync(new ConfirmRequest("Discard the changes?", "What you entered here is not saved.",
                "Discard changes", "Keep editing")
            {
                IsDestructive = true
            }))
        {
            return;
        }

        Photo.Discard();
        appState.ClearDraft<ProductDetailDraft>();
        await ReturnToListAsync();
    }

    // The draft stays: the edits are still on screen when this page appears again
    // Hidden for a role that may not issue; StartAsync refuses it too
    public bool CanIssue =>
        currentUser.CurrentUser is { } user && RolePermissions.CanRecord(user.Role, OperationDirection.Out);

    [RelayCommand]
    private Task StartReceiveAsync() => StartDocumentAsync(OperationDirection.In, Routes.Receive);

    [RelayCommand]
    private Task StartIssueAsync() => StartDocumentAsync(OperationDirection.Out, Routes.Issue);

    // Puts this product on the batch draft and opens it
    private async Task StartDocumentAsync(OperationDirection direction, string route)
    {
        if (_product is null)
        {
            return;
        }

        if (currentUser.CurrentUser is not { } user || OperationRules.RoleRefusal(user.Role, direction) is not null)
        {
            StatusMessage = "You cannot record this kind of operation.";
            logger.LogWarning("Denied: {Direction} draft for product {ProductId}", direction, _product.Id);
            return;
        }

        var draft = await drafts.LoadAsync(direction);
        draft.Add(_product.Barcode);
        await drafts.SaveAsync(draft);
        // The document opens with the cursor on this product's quantity
        await navigation.GoToAsync(route, new Dictionary<string, object> { [NavigationKeys.Barcode] = _product.Barcode });
    }

    [RelayCommand]
    private Task OpenOperationAsync() =>
        _product is null
            ? Task.CompletedTask
            : navigation.GoToAsync(Routes.Operation,
                new Dictionary<string, object> { [NavigationKeys.SelectedProduct] = _product });

    private bool CanSave() => CanEdit && IsEditing && _product is not null && Characteristics.All(c => c.Error.Length == 0);

    // Sends the product, as it is now, back so the list can update its row without reloading
    private async Task ReturnToListAsync()
    {
        var current = _product is null ? null : await catalog.GetSummaryAsync(_product.Id);
        if (current is null)
        {
            await navigation.GoToAsync(Routes.Back);
            return;
        }

        await navigation.GoToAsync(Routes.Back,
            new Dictionary<string, object> { [NavigationKeys.UpdatedProduct] = current });
    }

    private void ShowSummary(ProductSummary product)
    {
        Name = product.Name;
        Barcode = product.Barcode;
        Quantity = product.Quantity;
    }

    private void RebuildCharacteristics(IReadOnlyList<CharacteristicValueItem> sources)
    {
        foreach (var item in Characteristics)
        {
            item.PropertyChanged -= OnItemChanged;
        }

        Characteristics.Clear();
        foreach (var source in sources)
        {
            var item = new CharacteristicEditItem(source, validator, CanEdit);
            item.PropertyChanged += OnItemChanged;
            Characteristics.Add(item);
        }

        _stored = sources.ToDictionary(s => s.CharacteristicId, s => s.Value);
    }

    // Puts the unsaved edits of this product (if any) back into the freshly built items
    private void RestoreDraft()
    {
        if (!CanEdit || appState.GetDraft<ProductDetailDraft>() is not { } draft || draft.Barcode != _product!.Barcode)
        {
            return;
        }

        foreach (var item in Characteristics.Where(i => draft.Values.ContainsKey(i.CharacteristicId)))
        {
            item.Value = draft.Values[item.CharacteristicId];
        }

        if (draft.PhotoChanged)
        {
            Photo.Restore(draft.PhotoPath);
        }

        // Unsaved edits are shown where they can be saved
        IsEditing = true;
    }

    // The draft is the set of values that differ from the stored ones plus an unsaved photo; none = no draft
    private void SaveDraft()
    {
        var edits = Characteristics
            .Where(c => c.Value != _stored.GetValueOrDefault(c.CharacteristicId, string.Empty))
            .ToDictionary(c => c.CharacteristicId, c => c.Value);

        if (edits.Count == 0 && !Photo.IsChanged)
        {
            appState.ClearDraft<ProductDetailDraft>();
        }
        else
        {
            appState.SetDraft(new ProductDetailDraft(_product!.Barcode, edits, Photo.IsChanged, Photo.IsChanged ? Photo.Path : null));
        }
    }

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        SaveCommand.NotifyCanExecuteChanged();
        if (e.PropertyName == nameof(CharacteristicEditItem.Value) && !_rebuilding && CanEdit)
        {
            SaveDraft();
        }
    }
}
