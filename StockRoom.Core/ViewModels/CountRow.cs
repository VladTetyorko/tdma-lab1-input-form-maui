using CommunityToolkit.Mvvm.ComponentModel;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// One product of the stock count: what the database says (the count is not blind), what was counted, the difference
public sealed partial class CountRow(ProductSummary product, string countedText) : ObservableObject
{
    public ProductSummary Product { get; } = product;

    public string Name => Product.Name;

    public string Barcode => Product.Barcode;

    public int OnHand => Product.Quantity;

    public bool IsOutOfStock => OnHand <= 0;

    public string AutomationId => $"Count_{Barcode}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Counted))]
    [NotifyPropertyChangedFor(nameof(Difference))]
    [NotifyPropertyChangedFor(nameof(DifferenceText))]
    [NotifyPropertyChangedFor(nameof(IsDifferent))]
    [NotifyPropertyChangedFor(nameof(IsMissing))]
    public partial string CountedText { get; set; } = countedText;

    // Null while not counted (an empty or unusable field)
    public int? Counted => int.TryParse(CountedText, out var counted) && counted >= 0 ? counted : null;

    public int? Difference => Counted - OnHand;

    // "+2", "−1", "✓" or "—"
    public string DifferenceText => Difference switch
    {
        null => ProductTable.Missing,
        0 => "✓",
        > 0 => $"+{Difference}",
        _ => $"−{-Difference}"
    };

    public bool IsDifferent => Difference is not null and not 0;

    public bool IsMissing => Difference < 0;

    // "Changed since you counted: recount"
    [ObservableProperty]
    public partial string Problem { get; set; } = string.Empty;
}
