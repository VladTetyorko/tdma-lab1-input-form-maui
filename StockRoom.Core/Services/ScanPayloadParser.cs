namespace Mobile_apps_L1_Model_view.Services;

// Turns the raw text of a scan (camera or typed) into the product barcode it stands for.
// Accepted: EAN-13, UPC-A, UPC-E, EAN-8, ISBN-10 (see BarcodeNormalizer) and a stockroom://product/{barcode}
// link (what a StockRoom QR code carries). Anything else is "not a StockRoom code".
public static class ScanPayloadParser
{
    public const string EmptyMessage = "Enter a barcode.";

    public const string NotStockRoomCodeMessage =
        "Not a StockRoom code. Scan an EAN, UPC or ISBN barcode, or a StockRoom QR code.";

    public static Result<string> Parse(string? raw)
    {
        var text = raw?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return Result<string>.Failure(EmptyMessage);
        }

        var barcode = BarcodeNormalizer.ToEan13(DeepLinkParser.Parse(text)?.Barcode ?? text);
        return barcode is null ? Result<string>.Failure(NotStockRoomCodeMessage) : Result<string>.Success(barcode);
    }
}
