using CommunityToolkit.Mvvm.ComponentModel;
using Mobile_apps_L1_Model_view.Services;

namespace Mobile_apps_L1_Model_view.ViewModels;

// A suggestion as a row of the drop-down; the highlight moves with the arrow keys
public sealed partial class QuickSearchRow(ProductSuggestion suggestion) : ObservableObject
{
    public ProductSuggestion Suggestion { get; } = suggestion;

    public string AutomationId => $"Suggestion_{Suggestion.Code}";

    [ObservableProperty]
    public partial bool IsHighlighted { get; set; }
}
