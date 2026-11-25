using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

// The shake sensor starts when the product list or the scanner appears and stops when it disappears
public class ShakeLifecycleTests
{
    private readonly TestStore _store = new();
    private readonly FakeBarcodeScanner _scanner = new();

    private ProductListViewModel CreateList() => new(
        _store.Catalog, _store.CharacteristicService, _store.ProductService, _store.CurrentUser, _store.Startup, _store.Alerts, _store.Guard, _store.Navigation,
        _store.ShakeGesture, _store.Logger<ProductListViewModel>());

    private ScannerViewModel CreateScanner() => new(
        _scanner, _store.Catalog, _store.CurrentUser, _store.Guard, _store.Navigation, new FakeProductLookup(),
        _store.ShakeGesture, _store.Logger<ScannerViewModel>());

    [Fact]
    public async Task List_Appearing_StartsTheSensor_AndAShakeOpensTheScanner()
    {
        await _store.SignInAsync(Role.Storekeeper);
        var list = CreateList();

        list.OnAppearing();
        _store.ShakeDetector.Shake();

        Assert.True(_store.ShakeDetector.IsRunning);
        Assert.Equal(Routes.Scanner, Assert.Single(_store.Navigation.RequestedRoutes));
        Assert.Equal(1, _store.MainThread.Invocations);
    }

    [Fact]
    public async Task List_Disappearing_StopsTheSensor_AndAShakeDoesNothing()
    {
        await _store.SignInAsync(Role.Storekeeper);
        var list = CreateList();
        list.OnAppearing();

        list.OnDisappearing();
        _store.ShakeDetector.Shake();

        Assert.False(_store.ShakeDetector.IsRunning);
        Assert.Equal(0, _store.ShakeDetector.HandlerCount);
        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task List_AppearingAgainAfterDisappearing_StartsTheSensorAgain()
    {
        await _store.SignInAsync(Role.Storekeeper);
        var list = CreateList();

        list.OnAppearing();
        list.OnDisappearing();
        list.OnAppearing();

        Assert.True(_store.ShakeDetector.IsRunning);
        Assert.Equal(2, _store.ShakeDetector.Starts);
        Assert.Equal(1, _store.ShakeDetector.Stops);
    }

    [Fact]
    public void List_NotSignedIn_DoesNotStartTheSensor()
    {
        var list = CreateList();

        list.OnAppearing();

        Assert.Equal(0, _store.ShakeDetector.Starts);
    }

    [Fact]
    public async Task List_ShakeSwitchedOffInSettings_DoesNotStartTheSensor()
    {
        await _store.SignInAsync(Role.Storekeeper);
        _store.ShakeSettings.IsEnabled = false;
        var list = CreateList();

        list.OnAppearing();

        Assert.Equal(0, _store.ShakeDetector.Starts);
    }

    [Fact]
    public async Task List_RepeatedShakes_OpenTheScannerOnce_UntilTheWindowPasses()
    {
        await _store.SignInAsync(Role.Storekeeper);
        var list = CreateList();
        list.OnAppearing();

        _store.ShakeDetector.Shake();
        _store.Clock.UtcNow = _store.Clock.UtcNow.AddMilliseconds(300);
        _store.ShakeDetector.Shake();
        _store.ShakeDetector.Shake();
        Assert.Single(_store.Navigation.RequestedRoutes);

        _store.Clock.UtcNow = _store.Clock.UtcNow + ShakeGesture.DebounceWindow;
        _store.ShakeDetector.Shake();
        Assert.Equal(2, _store.Navigation.RequestedRoutes.Count);
    }

    [Fact]
    public async Task List_AccelerometerMissing_StillAppearsNormally()
    {
        await _store.SignInAsync(Role.Storekeeper);
        _store.ShakeDetector.IsSupported = false;
        var list = CreateList();

        list.OnAppearing();
        await list.LoadCommand.ExecuteAsync(null);

        Assert.Equal(0, _store.ShakeDetector.HandlerCount);
        Assert.NotEmpty(_store.Logs);
    }

    [Fact]
    public async Task Scanner_Appearing_StartsTheSensor_AndAShakeScansAgain()
    {
        await _store.SignInAsync(Role.Storekeeper);
        var scanner = CreateScanner();
        scanner.OnAppearing();
        Assert.Equal(1, _scanner.AccessRequests);

        _store.ShakeDetector.Shake();

        Assert.True(_store.ShakeDetector.IsRunning);
        Assert.Equal(2, _scanner.AccessRequests);
        Assert.Empty(_store.Navigation.RequestedRoutes);
    }

    [Fact]
    public async Task Scanner_Disappearing_StopsTheSensorAndTheCamera()
    {
        await _store.SignInAsync(Role.Storekeeper);
        var scanner = CreateScanner();
        scanner.OnAppearing();

        scanner.OnDisappearing();
        _store.ShakeDetector.Shake();

        Assert.False(_store.ShakeDetector.IsRunning);
        Assert.False(scanner.IsCameraActive);
        Assert.Equal(1, _scanner.AccessRequests);
    }

    [Fact]
    public async Task ListThenScanner_TheSensorKeepsRunning_WhateverTheOrderOfAppearingAndDisappearing()
    {
        await _store.SignInAsync(Role.Storekeeper);
        var list = CreateList();
        var scanner = CreateScanner();
        list.OnAppearing();

        // The scanner appears before the list has finished disappearing
        scanner.OnAppearing();
        list.OnDisappearing();

        Assert.True(_store.ShakeDetector.IsRunning);
        scanner.OnDisappearing();
        Assert.False(_store.ShakeDetector.IsRunning);
    }
}
