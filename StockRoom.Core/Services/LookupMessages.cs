namespace Mobile_apps_L1_Model_view.Services;

// What the user reads about a lookup (Search, receive and issue lines, product form)
public static class LookupMessages
{
    public const string Searching = "Looking it up in open databases…";

    public static string Found(LookupResult result)
    {
        var details = result.Details.Length == 0 ? string.Empty : $" · {result.Details}";
        return $"Found in {result.Source}: {result.Name}{details}";
    }

    public static string Failure(LookupStatus status, string barcode, bool canAdd = true)
    {
        var message = status switch
        {
            LookupStatus.NotFound => $"No open database knows barcode {barcode}.",
            LookupStatus.Offline => "Cannot reach the open databases (offline or too slow). Try again later.",
            _ => $"Not every open database answered, and the others do not know barcode {barcode}. Try again later."
        };
        return canAdd ? $"{message} You can add the product by hand." : message;
    }
}
