namespace Mobile_apps_L1_Model_view.Services;

// Each method returns an error message for the user, or null when the value is valid
public interface IProductValidator
{
    string? ValidateName(string? name);

    // Format only (13 digits and a correct check digit): cheap, safe to call on every keystroke
    string? ValidateBarcodeFormat(string? barcode);

    // Format plus uniqueness among existing products; excludeProductId is the product being edited,
    // which may keep its own barcode
    Task<string?> ValidateBarcodeAsync(string? barcode, Guid? excludeProductId = null);
}
