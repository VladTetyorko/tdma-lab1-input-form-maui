namespace Mobile_apps_L1_Model_view.Services;

// What the person scanning feels without looking at the screen. Never throws: missing hardware is not an error
public interface IScanFeedback
{
    void Success();

    void Error();

    // Tells a screen reader what was just done; nothing is shown
    void Announce(string text);
}
