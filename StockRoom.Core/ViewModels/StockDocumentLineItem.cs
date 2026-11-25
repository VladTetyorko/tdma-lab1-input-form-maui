using Mobile_apps_L1_Model_view.Models;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// One line as the page shows it. Product is null when no product has the barcode
public sealed record StockDocumentLineItem(string Barcode, ProductSummary? Product, int Quantity, OperationDirection Direction)
{
    public bool IsUnknown => Product is null;

    public bool ExceedsStock => Product is not null && OperationRules.ExceedsStock(Direction, Quantity, Product.Quantity);

    // An invalid line blocks the commit; the sentence in Problem says why
    public bool IsInvalid => IsUnknown || ExceedsStock;

    public string Name => Product?.Name ?? "Unknown barcode";

    public int OnHand => Product?.Quantity ?? 0;

    public int After => Direction == OperationDirection.In ? OnHand + Quantity : OnHand - Quantity;

    public string StockText => Product is null ? Barcode : $"on hand {OnHand} → {After}";

    public string Problem => IsUnknown
        ? $"No product has barcode {Barcode}"
        : ExceedsStock ? $"Only {OnHand} on hand" : string.Empty;

    // For the quantity box: what the person types is applied by SetQuantityCommand
    public string QuantityText { get; set; } = Quantity.ToString();
}
