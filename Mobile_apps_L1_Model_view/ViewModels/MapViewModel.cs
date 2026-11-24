using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// The map page: every operation that was recorded with a location, as pins (a Map control where the platform has one)
// and as a list with an "Open in maps" button (everywhere, and the only view on Windows). Read-only, for every role
public partial class MapViewModel(
    IProductCatalog catalog,
    IMapLauncher maps,
    IDeviceFeedback feedback,
    ILogger<MapViewModel> logger) : ObservableObject, IPageLifecycleAware
{
    // Newest first; the Map control binds to this as its ItemsSource
    public ObservableCollection<LocatedOperation> Pins { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoPins))]
    public partial bool HasPins { get; set; }

    public bool HasNoPins => !HasPins;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public void OnAppearing() => LoadCommand.Execute(null);

    [RelayCommand]
    private async Task LoadAsync()
    {
        var located = await catalog.GetLocatedOperationsAsync();
        Pins.Clear();
        foreach (var operation in located)
        {
            Pins.Add(operation);
        }

        HasPins = Pins.Count > 0;
        StatusMessage = string.Empty;
        logger.LogDebug("Map loaded: {Count} operations with a location", Pins.Count);
    }

    [RelayCommand]
    private async Task OpenInMapsAsync(LocatedOperation? operation)
    {
        if (operation is null)
        {
            return;
        }

        var status = await maps.OpenAsync(operation.Location, operation.Label);
        StatusMessage = feedback.Report(DeviceFeature.Maps, status);
        if (status == DeviceStatus.Succeeded)
        {
            logger.LogInformation("Maps opened for operation {OperationId}", operation.OperationId);
        }
    }
}
