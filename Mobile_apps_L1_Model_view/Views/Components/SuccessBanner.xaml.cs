namespace Mobile_apps_L1_Model_view.Views.Components;

public partial class SuccessBanner : ContentView
{
    private static readonly TimeSpan Visible = TimeSpan.FromSeconds(6);

    public static readonly BindableProperty MessageProperty = BindableProperty.Create(nameof(Message), typeof(string),
        typeof(SuccessBanner), propertyChanged: (banner, _, value) => ((SuccessBanner)banner).Show((string?)value));

    private int _shown;

    public SuccessBanner()
    {
        InitializeComponent();
    }

    public string? Message
    {
        get => (string?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    private void Show(string? message)
    {
        var shown = ++_shown;
        MessageText.Text = message;
        IsVisible = !string.IsNullOrEmpty(message);
        if (!IsVisible)
        {
            return;
        }

        SemanticScreenReader.Announce(message!);
        Dispatcher.DispatchDelayed(Visible, () =>
        {
            // A newer message keeps its own time
            if (shown == _shown)
            {
                IsVisible = false;
            }
        });
    }
}
