using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;

namespace Mobile_apps_L1_Model_view.Tests.Services;

public class ProductServicePhotoTests
{
    private const string Barcode = "4006381333931";
    private const string Photo = "photos/4006381333931_20260115_093000.jpg";

    private readonly TestStore _store = new();

    [Fact]
    public async Task AddAsync_WithPhoto_StoresTheRelativePath()
    {
        await _store.SignInAsync(Role.Manager);

        var result = await _store.ProductService.AddAsync("Saw", Barcode, Photo);

        Assert.Equal(Photo, result.Value!.PhotoPath);
    }

    [Theory]
    [InlineData("C:/photos/a.jpg")]
    [InlineData("photos/../a.jpg")]
    [InlineData("/photos/a.jpg")]
    public async Task AddAsync_PhotoPathOutsideThePhotosFolder_IsRejected(string path)
    {
        await _store.SignInAsync(Role.Manager);

        var result = await _store.ProductService.AddAsync("Saw", Barcode, path);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProductService.InvalidPhotoMessage, result.Error);
        Assert.Empty(await _store.Products.GetAllAsync());
    }

    [Fact]
    public async Task UpdateAsync_WithoutPhotoArgument_KeepsThePhoto()
    {
        await _store.SignInAsync(Role.Manager);
        var product = (await _store.ProductService.AddAsync("Saw", Barcode, Photo)).Value!;

        var result = await _store.ProductService.UpdateAsync(product.Id, "Circular Saw", Barcode);

        Assert.Equal(Photo, result.Value!.PhotoPath);
    }

    [Fact]
    public async Task UpdateAsync_WithPhotoArgument_ReplacesOrRemovesIt()
    {
        await _store.SignInAsync(Role.Manager);
        var product = (await _store.ProductService.AddAsync("Saw", Barcode, Photo)).Value!;

        await _store.ProductService.UpdateAsync(product.Id, "Saw", Barcode, "photos/new.jpg");
        Assert.Equal("photos/new.jpg", (await _store.Products.GetByIdAsync(product.Id))!.PhotoPath);
        var removed = await _store.ProductService.UpdateAsync(product.Id, "Saw", Barcode, null);

        Assert.Null(removed.Value!.PhotoPath);
    }

    [Fact]
    public async Task SetPhotoAsync_Manager_SetsThenRemoves()
    {
        await _store.SignInAsync(Role.Manager);
        var product = (await _store.ProductService.AddAsync("Saw", Barcode)).Value!;

        var set = await _store.ProductService.SetPhotoAsync(product.Id, Photo);
        Assert.Equal(Photo, (await _store.Products.GetByIdAsync(product.Id))!.PhotoPath);
        var removed = await _store.ProductService.SetPhotoAsync(product.Id, null);

        Assert.True(set.IsSuccess);
        Assert.Null(removed.Value!.PhotoPath);
        Assert.Null((await _store.Products.GetByIdAsync(product.Id))!.PhotoPath);
    }

    [Theory]
    [InlineData(Role.Storekeeper)]
    [InlineData(Role.Driver)]
    public async Task SetPhotoAsync_NonManager_IsRefusedAndWarned(Role role)
    {
        var product = await _store.AddProductAsync("Saw", Barcode);
        await _store.SignInAsync(role);

        var result = await _store.ProductService.SetPhotoAsync(product.Id, Photo);

        Assert.False(result.IsSuccess);
        Assert.Contains("cannot change the photo of products", result.Error);
        Assert.Null((await _store.Products.GetByIdAsync(product.Id))!.PhotoPath);
        Assert.Contains(_store.Logs, l => l.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task SetPhotoAsync_UnknownProduct_Fails()
    {
        await _store.SignInAsync(Role.Manager);

        var result = await _store.ProductService.SetPhotoAsync(Guid.NewGuid(), Photo);

        Assert.Equal("Product not found.", result.Error);
    }

    [Fact]
    public async Task SetPhotoAsync_InvalidPath_Fails()
    {
        await _store.SignInAsync(Role.Manager);
        var product = (await _store.ProductService.AddAsync("Saw", Barcode)).Value!;

        var result = await _store.ProductService.SetPhotoAsync(product.Id, "../x.jpg");

        Assert.Equal(ProductService.InvalidPhotoMessage, result.Error);
    }

    [Theory]
    [InlineData(Role.Manager, true)]
    [InlineData(Role.Storekeeper, false)]
    [InlineData(Role.Driver, false)]
    public void RolePermissions_CanManagePhotos_OnlyManager(Role role, bool expected)
    {
        Assert.Equal(expected, RolePermissions.CanManagePhotos(role));
    }
}
