using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Tests.Models;

public class GeoPointTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(90, 180)]
    [InlineData(-90, -180)]
    [InlineData(50.4501, 30.5234)]
    public void Constructor_AcceptsPlacesOnEarth(double latitude, double longitude)
    {
        var point = new GeoPoint(latitude, longitude);

        Assert.Equal(latitude, point.Latitude);
        Assert.Equal(longitude, point.Longitude);
    }

    [Theory]
    [InlineData(90.0001, 0)]
    [InlineData(-90.1, 0)]
    [InlineData(0, 180.5)]
    [InlineData(0, -181)]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.PositiveInfinity)]
    public void Constructor_RejectsOutOfRangeAndNonNumbers(double latitude, double longitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(latitude, longitude));
        Assert.False(GeoPoint.IsValid(latitude, longitude));
        Assert.Null(GeoPoint.TryCreate(latitude, longitude));
    }

    [Fact]
    public void Equality_IsByValue()
    {
        Assert.Equal(new GeoPoint(1, 2), new GeoPoint(1, 2));
        Assert.NotEqual(new GeoPoint(1, 2), new GeoPoint(2, 1));
    }

    [Fact]
    public void ToString_UsesTheInvariantCulture()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("uk-UA");

            Assert.Equal("50.45010, 30.52340", new GeoPoint(50.4501, 30.5234).ToString());
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void JsonRoundTrip_KeepsTheValues()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new GeoPoint(50.4501, 30.5234));

        var back = System.Text.Json.JsonSerializer.Deserialize<GeoPoint>(json);

        Assert.Equal(new GeoPoint(50.4501, 30.5234), back);
    }
}
