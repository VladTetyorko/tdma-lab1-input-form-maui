using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mobile_apps_L1_Model_view.Services;

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
    IMapLauncher maps,
    IDeviceFeedback feedback,
    ILogger<ProductDetailViewModel> logger,
    IStockDocumentDraftStore drafts) : ObservableObject, INavigationParameterReceiver, IPageLifecycleAware
{
    private ProductSummary? _product;

    // Stored value of each characteristic as last loaded: an edit is "unsaved" when it differs
    private Dictionary<Guid, string> _stored = [];

    // True while items are being built or the draft applied (those assignments are not user edits)
    private bool _rebuilding;

    public ObservableCollection<CharacteristicEditItem> Characteristics { get; } = [];

    // The photo of the product; a change is staged and saved with Save (see ProductPhotoEditor)
    public ProductPhotoEditor Photo { get; } = photo;

    // Newest first
    public ObservableCollection<OperationHistoryItem> History { get; } = [];

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Barcode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int Quantity { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial bool CanEdit { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationKeys.SelectedProduct, out var value) && value is ProductSummary product)
        {
            _product = product;
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

        History.Clear();
        foreach (var line in details.History)
        {
            History.Add(line);
        }

        StatusMessage = string.Empty;
        SaveCommand.NotifyCanExecuteChanged();
        logger.LogDebug("Product {ProductId} details loaded: {Count} operations in the history", _product.Id, History.Count);
    }

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
        await ReturnToListAsync();
    }

    [RelayCommand]
    private async Task TakePhotoAsync()
    {
        StatusMessage = await Photo.TakeAsync(Barcode);
        SaveDraft();
    }

    [RelayCommand]
    private async Task PickPhotoAsync()
    {
        StatusMessage = await Photo.PickAsync(Barcode);
        SaveDraft();
    }

    [RelayCommand]
    private async Task RemovePhotoAsync()
    {
        StatusMessage = await Photo.RemoveAsync();
        SaveDraft();
    }

    // The maps app shows where an operation was recorded
    [RelayCommand]
    private async Task OpenLocationAsync(OperationHistoryItem? item)
    {
        if (item?.Location is not { } location)
        {
            return;
        }

        var status = await maps.OpenAsync(location, $"{item.Direction} {item.Quantity} x {Name}");
        StatusMessage = feedback.Report(DeviceFeature.Maps, status);
        if (status == DeviceStatus.Succeeded)
        {
            logger.LogInformation("Maps opened for an operation of product {ProductId}", _product?.Id);
        }
    }

    [RelayCommand]
    private Task GoBackAsync()
    {
        Photo.Discard();
        appState.ClearDraft<ProductDetailDraft>();
        return ReturnToListAsync();
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
        await navigation.GoToAsync(route);
    }

    [RelayCommand]
    private Task OpenOperationAsync() =>
        _product is null
            ? Task.CompletedTask
            : navigation.GoToAsync(Routes.Operation,
                new Dictionary<string, object> { [NavigationKeys.SelectedProduct] = _product });

    private bool CanSave() => CanEdit && _product is not null && Characteristics.All(c => c.Error.Length == 0);

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
