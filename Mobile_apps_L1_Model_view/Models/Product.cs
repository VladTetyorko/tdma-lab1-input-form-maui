namespace Mobile_apps_L1_Model_view.Models;

public class Product : OwnedEntity
{
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;

    // Photo of the item: path relative to the app data folder, null = no photo. Only the path is stored and synced
    public string? PhotoPath { get; set; }
}
