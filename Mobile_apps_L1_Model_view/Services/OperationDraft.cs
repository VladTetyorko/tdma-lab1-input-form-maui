using Mobile_apps_L1_Model_view.Models;

namespace Mobile_apps_L1_Model_view.Services;

// Unsaved content of the operation form; Barcode says which product it belongs to
// (a barcode, unlike an id, is the same after the app restarts)
public sealed record OperationDraft(
    string Barcode, OperationDirection Direction, int Quantity, string Counterparty, string Reason);
