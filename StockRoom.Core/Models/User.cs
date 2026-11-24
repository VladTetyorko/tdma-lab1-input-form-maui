namespace Mobile_apps_L1_Model_view.Models;

public class User : Entity
{
    public string FullName { get; set; } = string.Empty;
    public Role Role { get; set; }

    // The name typed on the login page; unique, compared ignoring case
    public string Login { get; set; } = string.Empty;

    // "pbkdf2-sha256$iterations$salt$hash". Never a password; never logged
    public string PasswordHash { get; set; } = string.Empty;
}
