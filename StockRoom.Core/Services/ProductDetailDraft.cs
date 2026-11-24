namespace Mobile_apps_L1_Model_view.Services;

// Characteristic values typed on the detail page and not saved yet (only those that differ from
// the stored value; characteristic id -> text) and a photo change (PhotoChanged, then PhotoPath is the new relative
// path, null = removed). Barcode says which product they belong to
public sealed record ProductDetailDraft(
    string Barcode, Dictionary<Guid, string> Values, bool PhotoChanged = false, string? PhotoPath = null);
