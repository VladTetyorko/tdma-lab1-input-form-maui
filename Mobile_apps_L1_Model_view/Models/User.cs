namespace Mobile_apps_L1_Model_view.Models;

public class User : Entity
{
    public string FullName { get; set; } = string.Empty;
    public Role Role { get; set; }
}
