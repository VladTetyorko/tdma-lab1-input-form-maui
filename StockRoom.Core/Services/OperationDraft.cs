using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// Unsaved content of the operation form; Barcode says which product it belongs to
// (a barcode, unlike an id, is the same after the app restarts).
// Counterparty is plain text here; IOperationDraftStore stores it encrypted.
// Location is the position attached so far (null = none); coordinates are not secret, like Reason
public sealed record OperationDraft(
    string Barcode, OperationDirection Direction, int Quantity, string Counterparty, string Reason,
    GeoPoint? Location = null);
