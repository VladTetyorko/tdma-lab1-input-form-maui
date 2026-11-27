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
    [InlineData(typeof(SearchViewModel))]
    public void PageViewModel_ImplementsIPageLifecycleAware(Type viewModel)
    {
        Assert.True(typeof(IPageLifecycleAware).IsAssignableFrom(viewModel));
    }

    [Fact]
    public async Task LoginPage_OnAppearing_SignsOutAndClearsTheForm()
    {
        await DemoAccounts.SignInAsync(_provider, Role.Driver);
        var login = _provider.GetRequiredService<LoginViewModel>();
        login.ErrorMessage = "old error";

        ((IPageLifecycleAware)login).OnAppearing();

        await Eventually.WaitAsync(() => login.ErrorMessage.Length == 0);
        Assert.Null(_provider.GetRequiredService<ICurrentUserService>().CurrentUser);
    }

    [Fact]
    public async Task ProductsPage_OnAppearing_LoadsTheProducts()
    {
        await DemoAccounts.SignInAsync(_provider, Role.Manager);
        var list = _provider.GetRequiredService<ProductListViewModel>();

        ((IPageLifecycleAware)list).OnAppearing();

        await Eventually.WaitAsync(() => list.Products.Count == 12);
    }

    [Fact]
    public async Task SearchPage_OnAppearing_StartsTheCamera()
    {
        var scanner = _provider.GetRequiredService<SearchViewModel>();

        ((IPageLifecycleAware)scanner).OnAppearing();

        await Eventually.WaitAsync(() => scanner.IsCameraActive);
        ((IPageLifecycleAware)scanner).OnDisappearing();
        Assert.False(scanner.IsCameraActive);
    }
}
