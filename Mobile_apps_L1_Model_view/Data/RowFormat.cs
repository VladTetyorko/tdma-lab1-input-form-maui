using System.Globalization;

namespace Mobile_apps_L1_Model_view.Data;

// The text formats of the database in one place: UUIDs, ISO-8601 UTC timestamps and enum names
public static class RowFormat
{
    public static string Text(Guid id) => id.ToString("D");

    // "2026-01-31T10:15:00.0000000Z": fixed width, so text order equals time order
    public static string Text(DateTime utc) =>
        utc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);

    public static string Text<TEnum>(TEnum value) where TEnum : struct, Enum => value.ToString();

    public static Guid ToGuid(string text) => Guid.Parse(text);

    public static DateTime ToTime(string text) =>
        DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

    public static TEnum ToEnum<TEnum>(string text) where TEnum : struct, Enum => Enum.Parse<TEnum>(text);
}
