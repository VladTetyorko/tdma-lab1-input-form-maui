using System.Globalization;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Converters;

// Our GeoPoint (a Models record without MAUI types) -> the Location a map pin needs
public sealed class GeoPointToLocationConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is GeoPoint point ? new Location(point.Latitude, point.Longitude) : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
