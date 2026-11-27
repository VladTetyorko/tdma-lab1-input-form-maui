namespace Mobile_apps_L1_Model_view.Models;

// A place on Earth in degrees (WGS 84). Immutable and always valid: latitude -90..90, longitude -180..180
public sealed record GeoPoint
{
    public GeoPoint(double latitude, double longitude)
    {
        if (!IsValid(latitude, longitude))
        {
            throw new ArgumentOutOfRangeException(nameof(latitude),
                $"Latitude must be -90..90 and longitude -180..180 (got {latitude}, {longitude}).");
        }

        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }

    public static bool IsValid(double latitude, double longitude) =>
        latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;

    // Null when the numbers are not a place (NaN, out of range)
    public static GeoPoint? TryCreate(double latitude, double longitude) =>
        IsValid(latitude, longitude) ? new GeoPoint(latitude, longitude) : null;

    // "50.45010, 30.52340": the same text everywhere, whatever the culture of the device
    public override string ToString() =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{Latitude:0.00000}, {Longitude:0.00000}");
}
