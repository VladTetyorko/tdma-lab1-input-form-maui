namespace Mobile_apps_L1_Model_view.Services;

// Read side: joins products, stock, characteristic values and history into view-ready records
public interface IProductCatalog
{
    Task<IReadOnlyList<ProductSummary>> GetSummariesAsync();

    // Null when the product does not exist
    Task<ProductSummary?> GetSummaryAsync(Guid productId);

    // Null when no product has this barcode (the scanner and deep links look products up this way)
    Task<ProductSummary?> GetSummaryByBarcodeAsync(string barcode);

    // Every characteristic is listed, with an empty value when the product has none
    Task<ProductDetails?> GetDetailsAsync(Guid productId);
}
