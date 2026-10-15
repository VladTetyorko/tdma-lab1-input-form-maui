namespace Mobile_apps_L1_Model_view.Services;

// Characteristic values typed on the detail page and not saved yet (only those that differ from
// the stored value; characteristic id -> text). Barcode says which product they belong to
public sealed record ProductDetailDraft(string Barcode, Dictionary<Guid, string> Values);
