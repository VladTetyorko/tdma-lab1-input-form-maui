using Microsoft.Extensions.DependencyInjection;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.EndToEnd;

// Flows that cross app restarts, deep links and the scanner, through the real container.
// Each "run" of the app is a new container; the preferences are the only thing they share, like on a device.
public class AppLifecycleFlowTests
{
    private const string NewProductBarcode = "4006381333931";
    private readonly string _drillBarcode = Ean13.WithCheckDigit("482000100001");

    [Fact]
    public async Task KilledWhileRecordingAnOperation_RestartSignsInAndResumesTheDraft()
    {
        var preferences = new FakePreferencesStore();
        var secrets = new FakeSecretStore();
        var firstNavigation = new FakeNavigationService();

        // First run: sign in, open a product, start an Out operation and leave it unsaved
        using (var first = TestContainer.Create(preferences, firstNavigation, secrets: secrets))
        {
            var lifecycle = first.GetRequiredService<IAppLifecycleService>();
            await lifecycle.RestoreAsync();
            await SignInAsync(first, Role.Storekeeper);

            var list = first.GetRequiredService<ProductListViewModel>();
            await list.LoadCommand.ExecuteAsync(null);
            var drill = Assert.Single(list.Products, p => p.Barcode == _drillBarcode);
            await list.OpenDetailsCommand.ExecuteAsync(drill);

            var detail = first.GetRequiredService<ProductDetailViewModel>();
            detail.ApplyQueryAttributes(LastParameters(firstNavigation));
            await detail.LoadCommand.ExecuteAsync(null);
            await detail.OpenOperationCommand.ExecuteAsync(null);

            var operation = first.GetRequiredService<OperationViewModel>();
            operation.ApplyQueryAttributes(LastParameters(firstNavigation));
            await operation.LoadCommand.ExecuteAsync(null);
            operation.SelectedDirection = OperationDirection.Out;
            operation.Quantity = 4;
            operation.Counterparty = "Acme Ltd";
            operation.Reason = "Site 7";

            // The app is stopped (sent to the background and possibly killed)
            firstNavigation.CurrentRoute = Routes.Operation;
            lifecycle.OnStopped();
            Assert.DoesNotContain("Acme Ltd", string.Concat(preferences.Values.Values));
        }

        // Second run: a new process reads the same preferences
        var secondNavigation = new FakeNavigationService();
        using var second = TestContainer.Create(preferences, secondNavigation, secrets: secrets);
        var restart = second.GetRequiredService<IAppLifecycleService>();

        var startRoute = await restart.RestoreAsync();
        Assert.Equal(Routes.Products, startRoute);
        Assert.Equal(Role.Storekeeper, second.GetRequiredService<ICurrentUserService>().CurrentUser!.Role);

        // The list appears first and then resumes the operation page with its draft
        var list2 = second.GetRequiredService<ProductListViewModel>();
        await list2.EnsureLoadedCommand.ExecuteAsync(null);
        Assert.Equal(Routes.Operation, secondNavigation.RequestedRoutes.Last());

        var operation2 = second.GetRequiredService<OperationViewModel>();
        operation2.ApplyQueryAttributes(LastParameters(secondNavigation));
        await operation2.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Cordless Drill 18V", operation2.ProductName);
        Assert.Equal(OperationDirection.Out, operation2.SelectedDirection);
        Assert.Equal(4, operation2.Quantity);
        Assert.Equal("Acme Ltd", operation2.Counterparty);
        Assert.Equal("Site 7", operation2.Reason);
        Assert.Equal(21, operation2.QuantityOnHand);
    }

    [Fact]
    public async Task DeepLinkWhileSignedOut_OpensTheProductAfterSignIn()
    {
        var navigation = new FakeNavigationService();
        using var provider = TestContainer.Create(new FakePreferencesStore(), navigation);
        await provider.GetRequiredService<IAppLifecycleService>().RestoreAsync();
        var link = $"stockroom://product/{_drillBarcode}";

        await provider.GetRequiredService<IDeepLinkHandler>().HandleAsync(link);

        Assert.Equal("Sign in first", provider.GetRequiredService<FakeAlertService>().Shown.Single().Title);
        Assert.Empty(navigation.RequestedRoutes);

        await SignInAsync(provider, Role.Driver);
        var list = provider.GetRequiredService<ProductListViewModel>();
        await list.EnsureLoadedCommand.ExecuteAsync(null);

        Assert.Equal(Routes.ProductDetail, navigation.RequestedRoutes.Last());
        var detail = provider.GetRequiredService<ProductDetailViewModel>();
        detail.ApplyQueryAttributes(LastParameters(navigation));
        await detail.LoadCommand.ExecuteAsync(null);
        Assert.Equal("Cordless Drill 18V", detail.Name);
        Assert.Equal(_drillBarcode, detail.Barcode);
    }

    [Fact]
    public async Task AppStartedFromADeepLink_OpensTheProductOnceTheListIsShown()
    {
        var navigation = new FakeNavigationService();
        using var provider = TestContainer.Create(new FakePreferencesStore(), navigation);
        var link = $"stockroom://product/{_drillBarcode}";
        provider.GetRequiredService<IDeepLinkHandler>().Defer(link);
        await provider.GetRequiredService<IAppLifecycleService>().RestoreAsync();
        await SignInAsync(provider, Role.Storekeeper);

        var list = provider.GetRequiredService<ProductListViewModel>();
        await list.EnsureLoadedCommand.ExecuteAsync(null);

        // The sign-in itself went to the list; the deep link is the page after it
        Assert.Equal(new[] { Routes.Absolute(Routes.Products), Routes.ProductDetail }, navigation.RequestedRoutes);
        Assert.Equal(_drillBarcode,
            Assert.IsType<ProductSummary>(LastParameters(navigation)[NavigationKeys.SelectedProduct]).Barcode);
    }

    [Fact]
    public async Task Manager_ScansAnUnknownBarcode_AddsTheProduct_AndRecordsStockFromTheScanner()
    {
        var navigation = new FakeNavigationService();
        var scanner = new FakeBarcodeScanner { IsCameraSupported = false };
        using var provider = TestContainer.Create(new FakePreferencesStore(), navigation, scanner);
        await provider.GetRequiredService<IAppLifecycleService>().RestoreAsync();
        await SignInAsync(provider, Role.Manager);

        var list = provider.GetRequiredService<ProductListViewModel>();
        await list.LoadCommand.ExecuteAsync(null);
        await list.ScanCommand.ExecuteAsync(null);
        Assert.Equal(Routes.Scanner, navigation.RequestedRoutes.Last());

        // Windows: no camera, the barcode is typed; it matches no product
        var scan = provider.GetRequiredService<ScannerViewModel>();
        scan.OnAppearing();
        Assert.False(scan.IsCameraActive);
        scan.ManualBarcode = NewProductBarcode;
        await scan.LookUpCommand.ExecuteAsync(null);
        Assert.Equal(NewProductBarcode, scan.UnknownBarcode);
        Assert.True(scan.CanCreateProduct);

        // The form opens with the barcode filled in
        await scan.CreateProductCommand.ExecuteAsync(null);
        Assert.Equal(Routes.ProductForm, navigation.RequestedRoutes.Last());
        var form = provider.GetRequiredService<ProductFormViewModel>();
        form.ApplyQueryAttributes(LastParameters(navigation));
        form.OnAppearing();
        Assert.Equal(NewProductBarcode, form.Barcode);

        form.Name = "Cordless Saw";
        await form.SaveCommand.ExecuteAsync(null);
        Assert.Equal(Routes.Back, navigation.RequestedRoutes.Last());

        // Back on the scanner, the same barcode now finds the product and opens the operation page
        scan.ManualBarcode = NewProductBarcode;
        await scan.LookUpCommand.ExecuteAsync(null);
        Assert.Equal(Routes.Operation, navigation.RequestedRoutes.Last());

        var operation = provider.GetRequiredService<OperationViewModel>();
        operation.ApplyQueryAttributes(LastParameters(navigation));
        await operation.LoadCommand.ExecuteAsync(null);
        Assert.Equal("Cordless Saw", operation.ProductName);
        Assert.Equal(0, operation.QuantityOnHand);

        operation.SelectedDirection = OperationDirection.In;
        operation.Quantity = 5;
        await operation.SaveCommand.ExecuteAsync(null);
        Assert.True(string.IsNullOrEmpty(operation.ErrorMessage));

        // Leaving the scanner tells the list that products changed, so it shows the new quantity
        await scan.GoBackCommand.ExecuteAsync(null);
        list.ApplyQueryAttributes(LastParameters(navigation));
        await list.EnsureLoadedCommand.ExecuteAsync(null);

        var saw = Assert.Single(list.Products, p => p.Barcode == NewProductBarcode);
        Assert.Equal("Cordless Saw", saw.Name);
        Assert.Equal(5, saw.Quantity);
    }

    [Fact]
    public async Task ThemeChoice_IsAppliedAgainAfterARestart()
    {
        var preferences = new FakePreferencesStore();
        using (var first = TestContainer.Create(preferences, new FakeNavigationService()))
        {
            await first.GetRequiredService<IAppLifecycleService>().RestoreAsync();
            first.GetRequiredService<SettingsViewModel>().SelectedTheme = ThemeSetting.Dark;
        }

        using var second = TestContainer.Create(preferences, new FakeNavigationService());
        await second.GetRequiredService<IAppLifecycleService>().RestoreAsync();

        Assert.Equal(ThemeSetting.Dark, second.GetRequiredService<IThemeService>().Current);
        Assert.Equal(ThemeSetting.Dark, second.GetRequiredService<FakeThemeApplier>().Applied.Last());
        Assert.Equal(ThemeSetting.Dark, second.GetRequiredService<SettingsViewModel>().SelectedTheme);
    }

    private static IDictionary<string, object> LastParameters(FakeNavigationService navigation) =>
        navigation.RequestedParameters.Last()!;

    private static async Task SignInAsync(IServiceProvider provider, Role role)
    {
        var login = provider.GetRequiredService<LoginViewModel>();
        await login.LoadCommand.ExecuteAsync(null);
        login.SelectedUser = login.Users.Single(u => u.Role == role);
        await login.SignInCommand.ExecuteAsync(null);
    }
}
