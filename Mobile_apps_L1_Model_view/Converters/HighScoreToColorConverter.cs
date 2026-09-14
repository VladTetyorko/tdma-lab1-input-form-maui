using System.Globalization;

namespace Mobile_apps_L1_Model_view.Converters;

public class HighScoreToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool isHighScore && isHighScore ? Colors.Green : Colors.Red;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}