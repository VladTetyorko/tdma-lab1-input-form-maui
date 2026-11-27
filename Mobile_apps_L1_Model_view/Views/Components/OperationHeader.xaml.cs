namespace Mobile_apps_L1_Model_view.Views.Components;

public partial class OperationHeader : ContentView
{
    public static readonly BindableProperty ShowByProperty = BindableProperty.Create(nameof(ShowBy), typeof(bool),
        typeof(OperationHeader), true,
        propertyChanged: (header, _, value) => ((OperationHeader)header).ByLabel.IsVisible = (bool)value);

    public OperationHeader()
    {
        InitializeComponent();
    }

    public bool ShowBy
    {
        get => (bool)GetValue(ShowByProperty);
        set => SetValue(ShowByProperty, value);
    }
}
