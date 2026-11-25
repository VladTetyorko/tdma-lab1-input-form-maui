using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

// Photo and map commands of the detail page
public class ProductDetailPhotoTests
{
    private const string Barcode = "4006381333931";

    private readonly TestStore _store = new();

    private ProductDetailViewModel CreateViewModel() => new(
        _store.Catalog, _store.CharacteristicService, _store.Validator, _store.CurrentUser, _store.Guard, _store.AppState,
        _store.Navigation, _store.ProductService, _store.PhotoEditor, _store.Maps, _store.Feedback,
        _store.Logger<ProductDetailViewModel>(), new StockDocumentDraftStore(_store.AppState, new AesFieldEncryptor(_store.Secrets), _store.Logger<StockDocumentDraftStore>()), _store.Clock);

    private async Task<(ProductDetailViewModel ViewModel, Product Product)> OpenAsync(Role role, string? photoPath = null)
    {
        await _store.SignInAsync(role);
        var product = await _store.AddProductAsync("Drill", Barcode);
        product.PhotoPath = photoPath;
        await _store.Products.UpdateAsync(product);
        return (await ReopenAsync(product), product);
    }

    private async Task<ProductDetailViewModel> ReopenAsync(Product product)
    {
        var viewModel = CreateViewModel();
        viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationKeys.SelectedProduct] = (await _store.Catalog.GetSummaryAsync(product.Id))!
        });
        await viewModel.LoadCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task Load_ShowsTheStoredPhoto()
    {
        _store.Photos.Existing.Add("photos/a.jpg");

        var (viewModel, _) = await OpenAsync(Role.Storekeeper, "photos/a.jpg");

        Assert.Equal("photos/a.jpg", viewModel.Photo.Path);
        Assert.NotNull(viewModel.Photo.ResolvedPath);
        Assert.False(viewModel.CanEdit);
    }

    [Fact]
    public async Task TakePhoto_IsStagedInTheDraft_AndSavedWithSave()
    {
        var (viewModel, product) = await OpenAsync(Role.Manager);

        await viewModel.TakePhotoCommand.ExecuteAsync(null);
        var staged = viewModel.Photo.Path;
        var draft = _store.AppState.GetDraft<ProductDetailDraft>()!;
        Assert.True(draft.PhotoChanged);
        Assert.Equal(staged, draft.PhotoPath);
        Assert.Null((await _store.Products.GetByIdAsync(product.Id))!.PhotoPath);

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(staged, (await _store.Products.GetByIdAsync(product.Id))!.PhotoPath);
        Assert.Null(_store.AppState.GetDraft<ProductDetailDraft>());
        var updated = Assert.IsType<ProductSummary>(_store.Navigation.RequestedParameters.Single()![NavigationKeys.UpdatedProduct]);
        Assert.Equal(staged, updated.PhotoPath);
    }

    [Fact]
    public async Task StagedPhoto_SurvivesThePageBeingCreatedAgain()
    {
        var (viewModel, product) = await OpenAsync(Role.Manager);
        await viewModel.PickPhotoCommand.ExecuteAsync(null);
        var staged = viewModel.Photo.Path;

        var reopened = await ReopenAsync(product);

        Assert.Equal(staged, reopened.Photo.Path);
        Assert.True(reopened.Photo.IsChanged);
    }

    [Fact]
    public async Task Back_DiscardsTheStagedPhotoAndItsFile()
    {
        var (viewModel, product) = await OpenAsync(Role.Manager);
        await viewModel.TakePhotoCommand.ExecuteAsync(null);
        var staged = viewModel.Photo.Path;

        await viewModel.GoBackCommand.ExecuteAsync(null);

        Assert.Equal([staged!], _store.Photos.Deleted);
        Assert.Null(_store.AppState.GetDraft<ProductDetailDraft>());
        Assert.Null((await _store.Products.GetByIdAsync(product.Id))!.PhotoPath);
    }

    [Fact]
    public async Task RemovePhoto_ThenSave_ClearsThePathAndDeletesTheFile()
    {
        var (viewModel, product) = await OpenAsync(Role.Manager, "photos/a.jpg");
        _store.Photos.Existing.Add("photos/a.jpg");

        await viewModel.RemovePhotoCommand.ExecuteAsync(null);
        var draft = _store.AppState.GetDraft<ProductDetailDraft>()!;
        Assert.True(draft.PhotoChanged);
        Assert.Null(draft.PhotoPath);
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Null((await _store.Products.GetByIdAsync(product.Id))!.PhotoPath);
        Assert.Equal(["photos/a.jpg"], _store.Photos.Deleted);
    }

    [Theory]
    [InlineData(DeviceStatus.Cancelled)]
    [InlineData(DeviceStatus.PermissionDenied)]
    [InlineData(DeviceStatus.NotSupported)]
    [InlineData(DeviceStatus.Failed)]
    public async Task TakePhoto_NotSucceeded_ShowsTheMessageAndLeavesNoDraft(DeviceStatus status)
    {
        var (viewModel, _) = await OpenAsync(Role.Manager);
        _store.Photos.NextResult = new PhotoResult(status, null, status == DeviceStatus.Failed ? new IOException("x") : null);

        await viewModel.TakePhotoCommand.ExecuteAsync(null);

        Assert.Equal(DeviceMessages.For(DeviceFeature.Camera, status), viewModel.StatusMessage);
        Assert.Null(_store.AppState.GetDraft<ProductDetailDraft>());
        Assert.Contains(_store.Logs, l => l.Category == nameof(DeviceFeedback));
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task PhotoCommands_AsNonManager_AreRefused_AndNothingChanges(Role role)
    {
        var (viewModel, product) = await OpenAsync(role);

        await viewModel.TakePhotoCommand.ExecuteAsync(null);
        Assert.Equal($"Your role ({role}) cannot change product photos.", viewModel.StatusMessage);
        await viewModel.PickPhotoCommand.ExecuteAsync(null);
        await viewModel.RemovePhotoCommand.ExecuteAsync(null);

        Assert.Equal($"Your role ({role}) cannot remove product photos.", viewModel.StatusMessage);
        Assert.Empty(_store.Photos.CaptureBarcodes);
        Assert.Empty(_store.Photos.PickBarcodes);
        Assert.Null((await _store.Products.GetByIdAsync(product.Id))!.PhotoPath);
        Assert.Equal(3, _store.Logs.Count(l => l.Level == LogLevel.Warning && l.Category == nameof(PermissionGuard)));
    }

    [Fact]
    public async Task OpenLocation_OpensTheMapsAppWithALabel()
    {
        var (viewModel, product) = await OpenAsync(Role.Driver);
        await _store.Stock.RecordAsync(product.Id, OperationDirection.In, 4, location: new GeoPoint(50.45, 30.52));
        await viewModel.LoadCommand.ExecuteAsync(null);

        await viewModel.OpenLocationCommand.ExecuteAsync(viewModel.History.Single());

        var (location, label) = Assert.Single(_store.Maps.Opened);
        Assert.Equal(new GeoPoint(50.45, 30.52), location);
        Assert.Equal("In 4 x Drill", label);
        Assert.Equal(string.Empty, viewModel.StatusMessage);
    }

    [Fact]
    public async Task OpenLocation_NoMapsApp_ShowsAMessageAndLogsAWarning()
    {
        var (viewModel, product) = await OpenAsync(Role.Driver);
        await _store.Stock.RecordAsync(product.Id, OperationDirection.In, 4, location: new GeoPoint(50.45, 30.52));
        await viewModel.LoadCommand.ExecuteAsync(null);
        _store.Maps.Result = DeviceStatus.NotSupported;

        await viewModel.OpenLocationCommand.ExecuteAsync(viewModel.History.Single());

        Assert.Equal("No maps app is available on this device.", viewModel.StatusMessage);
        Assert.Contains(_store.Logs, l => l.Level == LogLevel.Warning && l.Category == nameof(DeviceFeedback));
    }

    [Fact]
    public async Task OpenLocation_OperationWithoutALocation_DoesNothing()
    {
        var (viewModel, product) = await OpenAsync(Role.Driver);
        await _store.Stock.RecordAsync(product.Id, OperationDirection.In, 4);
        await viewModel.LoadCommand.ExecuteAsync(null);

        await viewModel.OpenLocationCommand.ExecuteAsync(viewModel.History.Single());

        Assert.Empty(_store.Maps.Opened);
    }
}
