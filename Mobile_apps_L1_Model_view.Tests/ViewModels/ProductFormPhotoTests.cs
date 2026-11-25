using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

// The photo commands of the product form: taking, picking, removing, drafts, saving and leaving
public class ProductFormPhotoTests
{
    private const string Barcode = "4006381333931";

    private readonly TestStore _store = new();

    private ProductFormViewModel CreateForm() => _store.ProductForm;

    private async Task<ProductFormViewModel> NewProductFormAsync(Role role = Role.Manager)
    {
        await _store.SignInAsync(role);
        var form = CreateForm();
        form.Name = "Cordless Saw";
        form.Barcode = Barcode;
        return form;
    }

    [Fact]
    public async Task TakePhoto_ShowsTheThumbnailAndKeepsTheRelativePathInTheDraft()
    {
        var form = await NewProductFormAsync();

        await form.TakePhotoCommand.ExecuteAsync(null);

        Assert.True(form.Photo.HasPhoto);
        Assert.NotNull(form.Photo.ResolvedPath);
        Assert.Equal(string.Empty, form.StatusMessage);
        var draft = _store.AppState.GetDraft<ProductFormDraft>()!;
        Assert.Equal(form.Photo.Path, draft.PhotoPath);
        Assert.False(Path.IsPathRooted(draft.PhotoPath));
    }

    [Fact]
    public async Task TakePhoto_WithoutAValidBarcode_AsksForOneAndLogsAWarning()
    {
        var form = await NewProductFormAsync();
        form.Barcode = "123";

        await form.TakePhotoCommand.ExecuteAsync(null);

        Assert.Equal("Enter a valid barcode first: the photo file is named after it.", form.StatusMessage);
        Assert.Empty(_store.Photos.CaptureBarcodes);
        Assert.Contains(_store.Logs, l => l.Level == LogLevel.Warning && l.Category == nameof(ProductFormViewModel));
    }

    [Fact]
    public async Task PickPhoto_Denied_ShowsTheMessage()
    {
        var form = await NewProductFormAsync();
        _store.Photos.NextResult = PhotoResult.Of(DeviceStatus.PermissionDenied);

        await form.PickPhotoCommand.ExecuteAsync(null);

        Assert.Equal(DeviceMessages.For(DeviceFeature.Photos, DeviceStatus.PermissionDenied), form.StatusMessage);
        Assert.False(form.Photo.HasPhoto);
    }

    [Fact]
    public async Task Save_NewProduct_StoresThePhotoPathAndClearsTheDraft()
    {
        var form = await NewProductFormAsync();
        await form.TakePhotoCommand.ExecuteAsync(null);
        var path = form.Photo.Path;

        await form.SaveCommand.ExecuteAsync(null);

        var product = Assert.Single(await _store.Products.GetAllAsync());
        Assert.Equal(path, product.PhotoPath);
        Assert.Null(_store.AppState.GetDraft<ProductFormDraft>());
        var summary = Assert.IsType<ProductSummary>(_store.Navigation.RequestedParameters.Single()![NavigationKeys.UpdatedProduct]);
        Assert.Equal(path, summary.PhotoPath);
        Assert.Empty(_store.Photos.Deleted);
    }

    [Fact]
    public async Task DraftWithAPhoto_IsRestoredWhenTheFormOpensAgain()
    {
        var form = await NewProductFormAsync();
        await form.TakePhotoCommand.ExecuteAsync(null);
        var path = form.Photo.Path;

        var reopened = CreateForm();
        reopened.OnAppearing();

        Assert.Equal(Barcode, reopened.Barcode);
        Assert.Equal(path, reopened.Photo.Path);
    }

    [Fact]
    public async Task RemovePhoto_UpdatesTheDraft()
    {
        var form = await NewProductFormAsync();
        await form.TakePhotoCommand.ExecuteAsync(null);

        await form.RemovePhotoCommand.ExecuteAsync(null);

        Assert.False(form.Photo.HasPhoto);
        Assert.Null(_store.AppState.GetDraft<ProductFormDraft>()!.PhotoPath);
    }

    [Fact]
    public async Task Cancel_DeletesThePhotoTakenForANewProduct()
    {
        var form = await NewProductFormAsync();
        await form.TakePhotoCommand.ExecuteAsync(null);
        var path = form.Photo.Path;

        await form.CancelCommand.ExecuteAsync(null);

        Assert.Equal([path!], _store.Photos.Deleted);
        Assert.Null(_store.AppState.GetDraft<ProductFormDraft>());
    }

    [Fact]
    public async Task Edit_ReplacingThePhoto_SavesTheNewOneAndDeletesTheOldFile()
    {
        await _store.SignInAsync(Role.Manager);
        var product = await _store.AddProductAsync("Saw", Barcode);
        product.PhotoPath = "photos/old.jpg";
        await _store.Products.UpdateAsync(product);
        _store.Photos.Existing.Add("photos/old.jpg");
        var form = CreateForm();
        form.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationKeys.SelectedProduct] = (await _store.Catalog.GetSummaryAsync(product.Id))!
        });
        Assert.Equal("photos/old.jpg", form.Photo.Path);

        await form.PickPhotoCommand.ExecuteAsync(null);
        var staged = form.Photo.Path;
        await form.SaveCommand.ExecuteAsync(null);

        Assert.Equal(staged, (await _store.Products.GetByIdAsync(product.Id))!.PhotoPath);
        Assert.Equal(["photos/old.jpg"], _store.Photos.Deleted);
    }

    [Fact]
    public async Task Edit_Cancel_KeepsTheStoredPhotoAndDeletesTheStagedOne()
    {
        await _store.SignInAsync(Role.Manager);
        var product = await _store.AddProductAsync("Saw", Barcode);
        product.PhotoPath = "photos/old.jpg";
        await _store.Products.UpdateAsync(product);
        var form = CreateForm();
        form.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationKeys.SelectedProduct] = (await _store.Catalog.GetSummaryAsync(product.Id))!
        });
        await form.TakePhotoCommand.ExecuteAsync(null);
        var staged = form.Photo.Path;

        await form.CancelCommand.ExecuteAsync(null);

        Assert.Equal([staged!], _store.Photos.Deleted);
        Assert.Equal("photos/old.jpg", (await _store.Products.GetByIdAsync(product.Id))!.PhotoPath);
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task PhotoCommands_AsNonManager_AreRefused(Role role)
    {
        var form = await NewProductFormAsync(role);

        await form.TakePhotoCommand.ExecuteAsync(null);
        Assert.Equal($"Your role ({role}) cannot change product photos.", form.StatusMessage);
        await form.PickPhotoCommand.ExecuteAsync(null);
        await form.RemovePhotoCommand.ExecuteAsync(null);

        Assert.False(form.CanManagePhotos);
        Assert.Equal($"Your role ({role}) cannot remove product photos.", form.StatusMessage);
        Assert.Empty(_store.Photos.CaptureBarcodes);
        Assert.Empty(_store.Photos.PickBarcodes);
    }

    [Fact]
    public async Task CanManagePhotos_Manager_IsTrue()
    {
        var form = await NewProductFormAsync();

        Assert.True(form.CanManagePhotos);
    }
}
