using CommunityToolkit.Mvvm.ComponentModel;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// A suggestion as a row of the drop-down; the highlight moves with the arrow keys
public sealed partial class QuickSearchRow(ProductSuggestion suggestion, bool issuing = false) : ObservableObject
{
    public ProductSuggestion Suggestion { get; } = suggestion;

    public string AutomationId => $"Suggestion_{Suggestion.Code}";

    // Issuing a product with no stock is possible to pick, but the row says so
    public bool IsUnavailable => issuing && Suggestion.IsOutOfStock;

    public bool IsAvailable => !IsUnavailable;

    [ObservableProperty]
    public partial bool IsHighlighted { get; set; }
}
