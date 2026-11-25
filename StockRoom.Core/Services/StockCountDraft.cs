namespace Mobile_apps_L1_Model_view.Services;

// What was counted for one product, and what the database said at that moment
public sealed record CountedProduct(int Counted, int OnHandWhenCounted);

// An unfinished stock count of this device: kept across restarts, not synced and not an entity
public sealed record StockCountDraft(DateTime StartedAt, Dictionary<Guid, CountedProduct> Counts);
