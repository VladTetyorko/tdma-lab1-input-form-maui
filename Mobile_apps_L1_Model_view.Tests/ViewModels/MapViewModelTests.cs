using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;
using Mobile_apps_L1_Model_view.Tests.Fakes;
using Mobile_apps_L1_Model_view.ViewModels;

namespace Mobile_apps_L1_Model_view.Tests.ViewModels;

public class MapViewModelTests
{
    private readonly TestStore _store = new();
    private readonly MapViewModel _viewModel;

    public MapViewModelTests()
    {
        _viewModel = new MapViewModel(_store.Catalog, _store.Maps, _store.Feedback, _store.Logger<MapViewModel>());
    }

    private async Task<Product> SeedAsync()
    {
        await _store.SignInAsync(Role.Driver, "Ivan Driver");
        var product = await _store.AddProductAsync("Drill", "4006381333931");
        await _store.Stock.RecordAsync(product.Id, OperationDirection.In, 1);
        _store.Clock.UtcNow = _store.Clock.UtcNow.AddMinutes(1);
        await _store.Stock.RecordAsync(product.Id, OperationDirection.In, 2, location: new GeoPoint(50.45, 30.52));
        _store.Clock.UtcNow = _store.Clock.UtcNow.AddMinutes(1);
        await _store.Stock.RecordAsync(product.Id, OperationDirection.In, 3, location: new GeoPoint(48.0, 24.0));
        return product;
    }

    [Fact]
    public async Task Load_ListsOnlyOperationsWithALocation_NewestFirst()
    {
        await SeedAsync();

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(_viewModel.HasPins);
        Assert.False(_viewModel.HasNoPins);
        Assert.Equal(2, _viewModel.Pins.Count);
        Assert.Equal(new GeoPoint(48.0, 24.0), _viewModel.Pins[0].Location);
        Assert.Equal(new GeoPoint(50.45, 30.52), _viewModel.Pins[1].Location);
        Assert.Equal("Drill", _viewModel.Pins[0].ProductName);
    }

    [Fact]
    public async Task Load_NoLocatedOperations_ShowsTheEmptyState()
    {
        await _store.SignInAsync(Role.Driver);
        var product = await _store.AddProductAsync("Drill", "4006381333931");
        await _store.Stock.RecordAsync(product.Id, OperationDirection.In, 1);

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(_viewModel.HasPins);
        Assert.True(_viewModel.HasNoPins);
        Assert.Empty(_viewModel.Pins);
    }

    [Fact]
    public async Task OnAppearing_Loads_AndLoadingAgainReplacesThePins()
    {
        var product = await SeedAsync();
        _viewModel.OnAppearing();
        Assert.Equal(2, _viewModel.Pins.Count);

        _store.Clock.UtcNow = _store.Clock.UtcNow.AddMinutes(1);
        await _store.Stock.RecordAsync(product.Id, OperationDirection.In, 4, location: new GeoPoint(1, 1));
        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(3, _viewModel.Pins.Count);
    }

    [Fact]
    public async Task Pins_NotifyWhenTheyChange_SoTheMapControlCanFollow()
    {
        await SeedAsync();
        var changes = 0;
        _viewModel.Pins.CollectionChanged += (_, _) => changes++;

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(changes > 0);
    }

    [Fact]
    public async Task OpenInMaps_PassesThePlaceAndLabelToTheMapsApp()
    {
        await SeedAsync();
        await _viewModel.LoadCommand.ExecuteAsync(null);

        await _viewModel.OpenInMapsCommand.ExecuteAsync(_viewModel.Pins[0]);

        var (location, label) = Assert.Single(_store.Maps.Opened);
        Assert.Equal(new GeoPoint(48.0, 24.0), location);
        Assert.Equal("In 3 x Drill", label);
        Assert.Equal(string.Empty, _viewModel.StatusMessage);
    }

    [Theory]
    [InlineData(DeviceStatus.NotSupported, "No maps app is available on this device.", LogLevel.Warning)]
    [InlineData(DeviceStatus.Failed, "The maps app could not be opened.", LogLevel.Error)]
    public async Task OpenInMaps_Problem_ShowsOneMessageAndLogsOneLine(DeviceStatus status, string message, LogLevel level)
    {
        await SeedAsync();
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _store.Maps.Result = status;

        await _viewModel.OpenInMapsCommand.ExecuteAsync(_viewModel.Pins[0]);

        Assert.Equal(message, _viewModel.StatusMessage);
        Assert.Equal(level, Assert.Single(_store.Logs, l => l.Category == nameof(DeviceFeedback)).Level);
    }

    [Fact]
    public async Task OpenInMaps_WithoutAPin_DoesNothing()
    {
        await _viewModel.OpenInMapsCommand.ExecuteAsync(null);

        Assert.Empty(_store.Maps.Opened);
    }

    [Fact]
    public async Task EveryRoleMayOpenTheMapPage()
    {
        foreach (var role in Enum.GetValues<Role>())
        {
            await _store.SignInAsync(role);
            await _viewModel.LoadCommand.ExecuteAsync(null);
        }

        Assert.DoesNotContain(_store.Logs, l => l.Level >= LogLevel.Warning);
    }
}
