namespace Mobile_apps_L1_Model_view.Services;

// The on-screen keyboard. Hiding it keeps the focus, so a USB or Bluetooth scanner still types into the box
public interface IKeyboard
{
    void Hide();
}
