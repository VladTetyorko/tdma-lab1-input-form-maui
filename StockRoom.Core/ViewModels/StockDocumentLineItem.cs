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

    public bool IsOutOfStock => Product is not null && Product.Quantity <= 0;

    public int After => Direction == OperationDirection.In ? OnHand + Quantity : OnHand - Quantity;

    // Stock never goes below zero: what is missing is said in words
    public string StockText => Product is null ? Barcode
        : ExceedsStock ? $"on hand {OnHand} → 0, short by {Quantity - OnHand}"
        : $"on hand {OnHand} → {After}";

    public string Problem => IsUnknown
        ? $"No product has barcode {Barcode}"
        : !ExceedsStock ? string.Empty
        : OnHand <= 0 ? "Out of stock" : $"Only {OnHand} on hand";

    // For the quantity box: what the person types is applied by SetQuantityCommand
    public string QuantityText { get; set; } = Quantity.ToString();
}
