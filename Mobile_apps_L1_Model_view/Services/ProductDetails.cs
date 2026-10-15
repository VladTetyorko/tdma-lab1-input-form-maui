namespace Mobile_apps_L1_Model_view.Services;

// Everything the detail page shows; History is newest first
public sealed record ProductDetails(
    ProductSummary Summary,
    IReadOnlyList<CharacteristicValueItem> Characteristics,
    IReadOnlyList<OperationHistoryItem> History);
