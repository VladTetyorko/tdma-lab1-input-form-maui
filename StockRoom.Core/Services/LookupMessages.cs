namespace Mobile_apps_L1_Model_view.Services;

// What to tell the user when Open Food Facts gave no product (scanner page and product form)
public static class LookupMessages
{
    public static string Failure(LookupStatus status, string barcode) => status switch
    {
        LookupStatus.NotFound => $"Open Food Facts does not know barcode {barcode}. You can add the product by hand.",
        LookupStatus.Offline => "Cannot reach Open Food Facts (offline or too slow). Try again later or add the product by hand.",
        _ => "Open Food Facts gave no usable answer. Try again later or add the product by hand."
    };
}
