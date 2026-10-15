using Mobile_apps_L1_Model_view.Data;

namespace Mobile_apps_L1_Model_view.Services;

public sealed class ProductValidator(IProductRepository products) : IProductValidator
{
    public const int MaxNameLength = 100;

    public string? ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required.";
        }

        return name.Trim().Length > MaxNameLength
            ? $"Name must be at most {MaxNameLength} characters."
            : null;
    }

    public string? ValidateBarcodeFormat(string? barcode)
    {
        var code = barcode?.Trim();
        if (string.IsNullOrEmpty(code))
        {
            return "Barcode is required.";
        }

        return Ean13.IsValid(code) ? null : "Barcode must be a valid EAN-13 (13 digits with a correct check digit).";
    }

    public async Task<string?> ValidateBarcodeAsync(string? barcode)
    {
        var formatError = ValidateBarcodeFormat(barcode);
        if (formatError is not null)
        {
            return formatError;
        }

        return await products.GetByBarcodeAsync(barcode!.Trim()) is null
            ? null
            : "A product with this barcode already exists.";
    }
}
