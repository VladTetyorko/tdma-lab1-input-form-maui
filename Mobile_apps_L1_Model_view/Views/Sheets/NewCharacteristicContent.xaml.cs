namespace Mobile_apps_L1_Model_view.Views.Sheets;

public partial class NewCharacteristicContent : ContentView
{
    public NewCharacteristicContent()
    {
        InitializeComponent();
        // The first field of a sheet has the cursor
        NameField.Loaded += (_, _) => Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(250), NameField.FocusInput);
    }
}
