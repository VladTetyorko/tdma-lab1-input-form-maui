using System.Globalization;

namespace Mobile_apps_L1_Model_view.Converters;

// Out of stock (0 units) is shown in red; other quantities keep the style's color
public class QuantityToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int quantity && quantity == 0 ? Colors.Red : BindableProperty.UnsetValue;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
