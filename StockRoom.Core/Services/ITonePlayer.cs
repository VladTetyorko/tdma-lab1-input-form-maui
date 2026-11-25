namespace Mobile_apps_L1_Model_view.Services;

// The short system sounds of a scanner. Never throws; silent where the platform has none
public interface ITonePlayer
{
    void Accepted();

    void Refused();
}
