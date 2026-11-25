using System.Globalization;

namespace Mobile_apps_L1_Model_view.Converters;

// A resource key ("IconHome") -> the resource, for models that name an icon without knowing the glyph
public sealed class ResourceKeyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string key && Application.Current?.Resources.TryGetValue(key, out var resource) == true ? resource : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
