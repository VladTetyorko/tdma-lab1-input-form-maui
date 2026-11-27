using System.Globalization;
using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// "R-20261124-0001" for the first receipt of a day, "I-..." for issues. Counted on this device only:
// two devices can produce the same number, which a server would have to renumber
public static class DocumentNumberGenerator
{
    public static string Prefix(OperationDirection direction, DateTime utcNow) =>
        $"{(direction == OperationDirection.In ? 'R' : 'I')}-{utcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}-";

    public static string Number(string prefix, int alreadyUsed) =>
        prefix + (alreadyUsed + 1).ToString("D4", CultureInfo.InvariantCulture);
}
