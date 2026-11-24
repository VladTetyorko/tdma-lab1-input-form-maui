namespace Mobile_apps_L1_Model_view.Services;

// Turns the raw text of a scan (camera or typed) into the product barcode it stands for.
// Accepted: EAN-13, UPC-A (12 digits, padded with a leading 0 to EAN-13) and a stockroom://product/{barcode}
// link (what a StockRoom QR code carries). Anything else is "not a StockRoom code".
public static class ScanPayloadParser
{
    public const string EmptyMessage = "Enter a barcode.";

    public const string NotStockRoomCodeMessage =
        "Not a StockRoom code. Scan an EAN-13 or UPC-A barcode, or a StockRoom QR code.";

    public static Result<string> Parse(string? raw)
    {
        var text = raw?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return Result<string>.Failure(EmptyMessage);
        }

        var candidate = DeepLinkParser.Parse(text)?.Barcode ?? text;
        var barcode = ToEan13(candidate);
        return barcode is null ? Result<string>.Failure(NotStockRoomCodeMessage) : Result<string>.Success(barcode);
    }

    // Null when the digits are neither a valid EAN-13 nor a valid UPC-A
    private static string? ToEan13(string digits)
    {
        if (digits.Length == Ean13.Length - 1 && Ean13.IsValid("0" + digits))
        {
            return "0" + digits;
        }

        return Ean13.IsValid(digits) ? digits : null;
    }
}
