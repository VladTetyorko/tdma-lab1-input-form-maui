namespace Mobile_apps_L1_Model_view.Models;

public class Product : OwnedEntity
{
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
}
