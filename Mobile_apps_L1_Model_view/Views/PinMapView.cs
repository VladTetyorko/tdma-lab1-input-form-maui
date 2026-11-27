using System.Collections;
using System.Collections.Specialized;
#if ANDROID || IOS
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using MapControl = Microsoft.Maui.Controls.Maps.Map;
using Mobile_apps_L1_Model_view.Converters;
using Mobile_apps_L1_Model_view.Services;
#endif

namespace Mobile_apps_L1_Model_view.Views;

// An embedded map with a pin per LocatedOperation (Microsoft.Maui.Controls.Maps). The Map control exists on Android and
// iOS only: the package is referenced for those targets alone, and elsewhere (Windows) this view stays empty and
// hides itself, so the page shows its list with "Open in maps" buttons instead.
// Bind ItemsSource to the pins; the page needs no platform code.
public sealed class PinMapView : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(PinMapView),
        propertyChanged: (view, _, items) => ((PinMapView)view).OnItemsSourceChanged((IEnumerable?)items));

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

#if ANDROID || IOS
    private readonly MapControl _map = new()
    {
        IsShowingUser = false,
        ItemTemplate = new DataTemplate(() =>
        {
            var pin = new Pin();
            pin.SetBinding(Pin.LabelProperty, nameof(LocatedOperation.Label));
            pin.SetBinding(Pin.AddressProperty, nameof(LocatedOperation.Details));
            pin.SetBinding(Pin.LocationProperty, nameof(LocatedOperation.Location), converter: new GeoPointToLocationConverter());
            return pin;
        })
    };

    private INotifyCollectionChanged? _observed;

    public PinMapView()
    {
        Content = _map;
    }

    private void OnItemsSourceChanged(IEnumerable? items)
    {
        if (_observed is not null)
        {
            _observed.CollectionChanged -= OnItemsChanged;
        }

        _map.ItemsSource = items;
        _observed = items as INotifyCollectionChanged;
        if (_observed is not null)
        {
            _observed.CollectionChanged += OnItemsChanged;
        }

        ShowFirstPin();
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => ShowFirstPin();

    // Centres the map on the newest operation with a location
    private void ShowFirstPin()
    {
        if (ItemsSource?.OfType<LocatedOperation>().FirstOrDefault() is { } newest)
        {
            _map.MoveToRegion(MapSpan.FromCenterAndRadius(
                new Location(newest.Location.Latitude, newest.Location.Longitude), Distance.FromKilometers(25)));
        }
    }
#else
    // No Map control on this platform: nothing to show
    public PinMapView()
    {
        IsVisible = false;
    }

    private void OnItemsSourceChanged(IEnumerable? items)
    {
    }
#endif
}
