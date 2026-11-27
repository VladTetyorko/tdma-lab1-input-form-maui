namespace Mobile_apps_L1_Model_view.Services;

// Unsaved content of the add-product form; PhotoPath is relative to the app data folder (null = no photo)
public sealed record ProductFormDraft(string Name, string Barcode, string? PhotoPath = null);
