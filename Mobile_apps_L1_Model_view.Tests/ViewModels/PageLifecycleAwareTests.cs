using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class PageLifecycleAwareTests : IDisposable
{
    private readonly ServiceProvider _provider = TestContainer.Create(new FakePreferencesStore(), new FakeNavigationService());

    public void Dispose() => _provider.Dispose();

    [Theory]
    [InlineData(typeof(LoginViewModel))]
    [InlineData(typeof(ProductListViewModel))]
    [InlineData(typeof(ProductFormViewModel))]
    [InlineData(typeof(ProductDetailViewModel))]
    [InlineData(typeof(CharacteristicsViewModel))]
    [InlineData(typeof(OperationViewModel))]
    [InlineData(typeof(ScannerViewModel))]
    public void PageViewModel_ImplementsIPageLifecycleAware(Type viewModel)
    {
        Assert.True(typeof(IPageLifecycleAware).IsAssignableFrom(viewModel));
    }

    [Fact]
    public async Task LoginPage_OnAppearing_LoadsTheUsers()
    {
        var login = _provider.GetRequiredService<LoginViewModel>();

        ((IPageLifecycleAware)login).OnAppearing();

        await Eventually.WaitAsync(() => login.Users.Count == 3);
    }

    [Fact]
    public async Task ProductsPage_OnAppearing_LoadsTheProducts()
    {
        var users = _provider.GetRequiredService<LoginViewModel>();
        await users.LoadCommand.ExecuteAsync(null);
        users.SelectedUser = users.Users.Single(u => u.Role == Role.Manager);
        await users.SignInCommand.ExecuteAsync(null);
        var list = _provider.GetRequiredService<ProductListViewModel>();

        ((IPageLifecycleAware)list).OnAppearing();

        await Eventually.WaitAsync(() => list.Products.Count == 10);
    }

    [Fact]
    public async Task ScannerPage_OnAppearing_StartsTheCamera()
    {
        var scanner = _provider.GetRequiredService<ScannerViewModel>();

        ((IPageLifecycleAware)scanner).OnAppearing();

        Assert.True(scanner.IsCameraActive);
        ((IPageLifecycleAware)scanner).OnDisappearing();
        Assert.False(scanner.IsCameraActive);
    }
}
