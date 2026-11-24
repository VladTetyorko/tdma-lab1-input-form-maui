using System.Globalization;
using Mobile_apps_L1_Model_view.Converters;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Tests.Converters;

public class GeoPointToLocationConverterTests
{
    private readonly GeoPointToLocationConverter _converter = new();

    [Fact]
    public void Convert_GeoPoint_GivesALocationWithTheSameCoordinates()
    {
        var location = Assert.IsType<Location>(
            _converter.Convert(new GeoPoint(50.45, 30.52), typeof(Location), null, CultureInfo.InvariantCulture));

        Assert.Equal(50.45, location.Latitude);
        Assert.Equal(30.52, location.Longitude);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not a point")]
    public void Convert_AnythingElse_GivesNull(object? value)
    {
        Assert.Null(_converter.Convert(value, typeof(Location), null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ConvertBack_IsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() => _converter.ConvertBack(null, typeof(GeoPoint), null, CultureInfo.InvariantCulture));
    }
}
